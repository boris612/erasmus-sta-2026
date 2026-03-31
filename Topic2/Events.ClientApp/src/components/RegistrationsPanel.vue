<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import AutoComplete, { type AutoCompleteCompleteEvent } from 'primevue/autocomplete';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable, { type DataTablePageEvent, type DataTableSortEvent } from 'primevue/datatable';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import { eventsApi, lookupApi, registrationsApi, sportsApi } from '../api/eventsApi';
import { eventsCatalogVersion, peopleCatalogVersion, sportsCatalogVersion, touchEventsCatalog } from '../state/catalogState';
import { activeTab, consumePendingRegistrationEventId, pendingRegistrationEventId } from '../state/uiState';
import type { EventDto, IdName, RegistrationDto, RegistrationUpsertDto, SportDto } from '../api/types';
import { formatDateTime } from '../utils/dates';

const rows = ref<RegistrationDto[]>([]);
const totalRecords = ref(0);
const loading = ref(false);
const error = ref('');
const dialogVisible = ref(false);
const selectedPerson = ref<IdName<number> | null>(null);
const peopleSuggestions = ref<Array<IdName<number>>>([]);
const countries = ref<Array<IdName<string>>>([]);
const sports = ref<SportDto[]>([]);
const events = ref<EventDto[]>([]);
const page = ref(1);
const pageSize = ref(10);
const sort = ref('RegisteredAt');
const sortOrder = ref<1 | -1>(-1);
const confirm = useConfirm();
const toast = useToast();

const form = reactive<RegistrationDto>({
  id: 0,
  eventId: 0,
  personId: 0,
  sportId: 0,
  registeredAt: null,
  personName: '',
  personTranscription: '',
  personFirstNameTranscription: '',
  personLastNameTranscription: '',
  countryCode: '',
  countryName: '',
  sportName: ''
});

const eventOptions = computed(() =>
  events.value.map((item) => ({
    label: `${item.name} (${item.eventDate})`,
    value: item.id
  }))
);

const sportOptions = computed(() =>
  sports.value.map((item) => ({
    label: item.name,
    value: item.id
  }))
);

const countryOptions = computed(() =>
  countries.value.map((item) => ({
    label: item.name,
    value: item.id
  }))
);

const filters = ref({
  EventId: {
    operator: 'and',
    constraints: [{ value: null as number | null, matchMode: 'equals' }]
  },
  PersonName: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  },
  PersonLastNameTranscription: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  },
  PersonFirstNameTranscription: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  },
  CountryCode: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'equals' }]
  },
  SportName: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  }
});

function escapeFilterValue(value: string) {
  return value
    .replace(/\\/g, '\\\\')
    .replace(/\|/g, '\\|')
    .replace(/,/g, '\\,');
}

function toSieveOperator(matchMode?: string) {
  switch (matchMode) {
    case 'startsWith':
      return '_=*';
    case 'endsWith':
      return '_-=*';
    case 'equals':
      return '==*';
    case 'notEquals':
      return '!=*';
    case 'contains':
    default:
      return '@=*';
  }
}

function getConstraintValues(field: keyof typeof filters.value) {
  return filters.value[field].constraints
    .map((constraint) => (constraint.value ?? '').toString().trim())
    .filter((value) => value !== '');
}

function buildSameFieldFilter(field: string, operator: string, values: string[], logicalOperator: string) {
  if (values.length === 0) {
    return [];
  }

  if (values.length === 1) {
    return [`${field}${operator}${values[0]}`];
  }

  if (logicalOperator === 'or') {
    return [`${field}${operator}${values.join('|')}`];
  }

  return values.map((value) => `${field}${operator}${value}`);
}

function toRegistrationPayload(): RegistrationUpsertDto {
  return {
    id: form.id,
    eventId: form.eventId,
    personId: form.personId,
    sportId: form.sportId
  };
}

function buildFilters() {
  const result: string[] = [];
  const textFields = ['PersonName', 'PersonLastNameTranscription', 'PersonFirstNameTranscription', 'SportName'] as const;

  for (const field of textFields) {
    const groups = new Map<string, string[]>();
    for (const constraint of filters.value[field].constraints) {
      const value = (constraint.value ?? '').toString().trim();
      if (!value) {
        continue;
      }

      const key = constraint.matchMode ?? 'contains';
      const entries = groups.get(key) ?? [];
      entries.push(escapeFilterValue(value));
      groups.set(key, entries);
    }

    for (const [matchMode, values] of groups.entries()) {
      result.push(
        ...buildSameFieldFilter(field, toSieveOperator(matchMode), values, filters.value[field].operator)
      );
    }
  }

  result.push(
    ...buildSameFieldFilter(
      'EventId',
      '==',
      getConstraintValues('EventId').map((value) => escapeFilterValue(value)),
      filters.value.EventId.operator
    )
  );

  result.push(
    ...buildSameFieldFilter(
      'CountryCode',
      '==',
      getConstraintValues('CountryCode').map((value) => escapeFilterValue(value)),
      filters.value.CountryCode.operator
    )
  );

  return result.length > 0 ? result.join(',') : undefined;
}

async function loadAuxiliaryData() {
  const [loadedEvents, loadedSports, loadedCountries] = await Promise.all([
    eventsApi.list({ page: 1, pageSize: 500, sort: 'EventDate', sortOrder: 1 }),
    sportsApi.list({ page: 1, pageSize: 500, sort: 'Name', sortOrder: 1 }),
    lookupApi.countries()
  ]);

  events.value = loadedEvents.data ?? [];
  sports.value = loadedSports.data ?? [];
  countries.value = loadedCountries;
}

async function loadData() {
  loading.value = true;
  error.value = '';
  try {
    const response = await registrationsApi.list({
      page: page.value,
      pageSize: pageSize.value,
      sort: sort.value,
      sortOrder: sortOrder.value,
      filters: buildFilters()
    });
    rows.value = response.data ?? [];
    totalRecords.value = response.count;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load registrations.';
  } finally {
    loading.value = false;
  }
}

function resetForm() {
  form.id = 0;
  form.eventId = 0;
  form.personId = 0;
  form.sportId = 0;
  form.registeredAt = null;
  form.personName = '';
  form.personTranscription = '';
  form.personFirstNameTranscription = '';
  form.personLastNameTranscription = '';
  form.countryCode = '';
  form.countryName = '';
  form.sportName = '';
  selectedPerson.value = null;
}

function openCreate() {
  error.value = '';
  resetForm();
  dialogVisible.value = true;
}

function openCreateForEvent(eventId: number) {
  error.value = '';
  resetForm();
  form.eventId = eventId;
  dialogVisible.value = true;
}

async function openEdit(id: number) {
  error.value = '';
  try {
    const item = await registrationsApi.get(id);
    Object.assign(form, item);
    selectedPerson.value = { id: item.personId, name: item.personName, description: item.personTranscription };
    dialogVisible.value = true;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load the registration.';
  }
}

async function completePeopleLookup(event: AutoCompleteCompleteEvent) {
  const query = event.query?.trim() ?? '';
  if (!query) {
    peopleSuggestions.value = [];
    return;
  }

  try {
    const countryCode = filters.value.CountryCode.constraints[0].value || undefined;
    peopleSuggestions.value = await lookupApi.people(query, countryCode);
  } catch (err) {
    peopleSuggestions.value = [];
    error.value = err instanceof Error ? err.message : 'Unable to load people for lookup.';
    toast.add({ severity: 'error', summary: 'Registrations', detail: error.value, life: 5000 });
  }
}

async function save() {
  error.value = '';
  form.personId = selectedPerson.value?.id ?? 0;

  try {
    if (form.id > 0) {
      await registrationsApi.update(toRegistrationPayload());
      toast.add({ severity: 'success', summary: 'Registrations', detail: 'Registration updated successfully.', life: 3000 });
    } else {
      await registrationsApi.create(toRegistrationPayload());
      toast.add({ severity: 'success', summary: 'Registrations', detail: 'Registration created successfully.', life: 3000 });
    }

    touchEventsCatalog();
    dialogVisible.value = false;
    await loadData();
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Save failed.';
    toast.add({ severity: 'error', summary: 'Registrations', detail: error.value, life: 5000 });
  }
}

async function remove(id: number) {
  confirm.require({
    message: 'Delete this registration?',
    header: 'Confirmation',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Delete',
    rejectLabel: 'Cancel',
    acceptClass: 'p-button-danger',
    accept: async () => {
      error.value = '';
      try {
        await registrationsApi.remove(id);
        touchEventsCatalog();
        toast.add({ severity: 'success', summary: 'Registrations', detail: 'Registration deleted successfully.', life: 3000 });
        await loadData();
      } catch (err) {
        error.value = err instanceof Error ? err.message : 'Delete failed.';
        toast.add({ severity: 'error', summary: 'Registrations', detail: error.value, life: 5000 });
      }
    }
  });
}

async function downloadCertificate(id: number) {
  error.value = '';

  try {
    const file = await registrationsApi.downloadCertificate(id);
    const url = URL.createObjectURL(file.blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = file.fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Certificate download failed.';
    toast.add({ severity: 'error', summary: 'Registrations', detail: error.value, life: 5000 });
  }
}

function onPage(event: DataTablePageEvent) {
  page.value = (event.page ?? 0) + 1;
  pageSize.value = event.rows;
  void loadData();
}

function onSort(event: DataTableSortEvent) {
  sort.value = event.sortField as string;
  sortOrder.value = (event.sortOrder ?? 1) as 1 | -1;
  void loadData();
}

function onFilter() {
  page.value = 1;
  void loadData();
}

watch([eventsCatalogVersion, sportsCatalogVersion, peopleCatalogVersion], async () => {
  await loadAuxiliaryData();
});

watch([activeTab, pendingRegistrationEventId], async () => {
  if (activeTab.value !== 'registrations' || pendingRegistrationEventId.value === null) {
    return;
  }

  if (events.value.length === 0) {
    await loadAuxiliaryData();
  }

  const eventId = consumePendingRegistrationEventId();
  if (eventId !== null) {
    openCreateForEvent(eventId);
  }
});

defineExpose({
  openCreate
});

onMounted(async () => {
  await loadAuxiliaryData();
  await loadData();
});
</script>

<template>
  <div class="panel-card">
    <DataTable
      v-model:filters="filters"
      :value="rows"
      :loading="loading"
      dataKey="id"
      filterDisplay="menu"
      lazy
      paginator
      :rows="pageSize"
      :first="(page - 1) * pageSize"
      :total-records="totalRecords"
      :sort-field="sort"
      :sort-order="sortOrder"
      @filter="onFilter"
      @page="onPage"
      @sort="onSort"
    >
      <Column field="id" header="ID" sortable />
      <Column field="personName" header="Person" sortable sortField="PersonName" filterField="PersonName" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by person" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="personLastNameTranscription" header="Last name transcription" sortable sortField="PersonLastNameTranscription" filterField="PersonLastNameTranscription" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by last name" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="personFirstNameTranscription" header="First name transcription" sortable sortField="PersonFirstNameTranscription" filterField="PersonFirstNameTranscription" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by first name" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="countryName" header="Country" sortable filterField="CountryCode" :showFilterMatchModes="false" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <Select
            v-model="filterModel.value"
            :options="countryOptions"
            option-label="label"
            option-value="value"
            placeholder="All countries"
            show-clear
            filter
            @change="filterCallback()"
          />
        </template>
      </Column>
      <Column field="sportName" header="Sport" sortable sortField="SportName" filterField="SportName" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by sport" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="eventId" header="Event" sortable sortField="EventId" filterField="EventId" :showFilterMatchModes="false" :filterMenuStyle="{ width: '16rem' }">
        <template #body="{ data }">
          {{ events.find((item) => item.id === data.eventId)?.name || data.eventId }}
        </template>
        <template #filter="{ filterModel, filterCallback }">
          <Select
            v-model="filterModel.value"
            :options="eventOptions"
            option-label="label"
            option-value="value"
            placeholder="All events"
            show-clear
            filter
            @change="filterCallback()"
          />
        </template>
      </Column>
      <Column field="registeredAt" header="Registered at" sortable>
        <template #body="{ data }">
          {{ formatDateTime(data.registeredAt) }}
        </template>
      </Column>
      <Column header="" style="width: 10rem">
        <template #body="{ data }">
          <div class="inline-actions">
            <Button icon="pi pi-download" text rounded @click="downloadCertificate(data.id)" />
            <Button icon="pi pi-pencil" text rounded @click="openEdit(data.id)" />
            <Button icon="pi pi-trash" text rounded severity="danger" @click="remove(data.id)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="dialogVisible" modal :style="{ width: '68rem' }" :header="form.id ? 'Edit registration' : 'New registration'">
      <div class="field-grid field-grid-registration">
        <div class="field field-span-2">
          <label>Event</label>
          <Select v-model="form.eventId" :options="eventOptions" option-label="label" option-value="value" />
        </div>
        <div class="field">
          <label>Sport</label>
          <Select v-model="form.sportId" :options="sportOptions" option-label="label" option-value="value" />
        </div>
        <div class="field" style="grid-column: 1 / -1">
          <label>Person</label>
          <AutoComplete
            v-model="selectedPerson"
            :suggestions="peopleSuggestions"
            optionLabel="name"
            :minLength="1"
            completeOnFocus
            dropdown
            dropdownMode="current"
            force-selection
            @complete="completePeopleLookup"
          >
            <template #option="{ option }">
              <div class="lookup-item">
                <span>{{ option.name }}</span>
                <small>{{ option.description || `ID: ${option.id}` }}</small>
              </div>
            </template>
          </AutoComplete>
        </div>
      </div>
      <template #footer>
        <Button label="Cancel" text @click="dialogVisible = false" />
        <Button label="Save" @click="save" />
      </template>
    </Dialog>
  </div>
</template>
