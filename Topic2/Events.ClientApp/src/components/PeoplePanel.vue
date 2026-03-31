<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable, { type DataTablePageEvent, type DataTableSortEvent } from 'primevue/datatable';
import DatePicker from 'primevue/datepicker';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import { lookupApi, peopleApi } from '../api/eventsApi';
import { touchPeopleCatalog } from '../state/catalogState';
import type { IdName, PersonDto } from '../api/types';
import { formatDateOnly, toDate, toDateOnlyString } from '../utils/dates';

const rows = ref<PersonDto[]>([]);
const totalRecords = ref(0);
const loading = ref(false);
const error = ref('');
const dialogVisible = ref(false);
const page = ref(1);
const pageSize = ref(10);
const sort = ref('LastName');
const sortOrder = ref<1 | -1>(1);
const birthDate = ref<Date | null>(null);
const countries = ref<Array<IdName<string>>>([]);
const confirm = useConfirm();
const toast = useToast();

const form = reactive<PersonDto>({
  id: 0,
  firstName: '',
  lastName: '',
  firstNameTranscription: '',
  lastNameTranscription: '',
  addressLine: '',
  postalCode: '',
  city: '',
  addressCountry: '',
  email: '',
  contactPhone: '',
  birthDate: '',
  documentNumber: '',
  countryCode: '',
  countryName: '',
  fullNameTranscription: '',
  registrationsCount: 0
});

const countryOptions = computed(() =>
  countries.value.map((country) => ({
    label: country.name,
    value: country.id
  }))
);

const filters = ref({
  Id: {
    operator: 'and',
    constraints: [{ value: null as number | null, matchMode: 'equals' }]
  },
  FirstName: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  },
  LastName: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  },
  FirstNameTranscription: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  },
  LastNameTranscription: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  },
  Email: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  },
  CountryCode: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'equals' }]
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

function buildFilters() {
  const result: string[] = [];
  const textFields = ['FirstName', 'LastName', 'FirstNameTranscription', 'LastNameTranscription', 'Email'] as const;

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
      'Id',
      '==',
      getConstraintValues('Id').map((value) => escapeFilterValue(value)),
      filters.value.Id.operator
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

async function loadCountries() {
  countries.value = await lookupApi.countries();
}

async function loadData() {
  loading.value = true;
  error.value = '';
  try {
    const response = await peopleApi.list({
      page: page.value,
      pageSize: pageSize.value,
      sort: sort.value,
      sortOrder: sortOrder.value,
      filters: buildFilters()
    });
    rows.value = response.data ?? [];
    totalRecords.value = response.count;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load people.';
  } finally {
    loading.value = false;
  }
}

function resetForm() {
  form.id = 0;
  form.firstName = '';
  form.lastName = '';
  form.firstNameTranscription = '';
  form.lastNameTranscription = '';
  form.addressLine = '';
  form.postalCode = '';
  form.city = '';
  form.addressCountry = '';
  form.email = '';
  form.contactPhone = '';
  form.birthDate = '';
  form.documentNumber = '';
  form.countryCode = '';
  form.countryName = '';
  form.fullNameTranscription = '';
  form.registrationsCount = 0;
  birthDate.value = null;
}

function openCreate() {
  error.value = '';
  resetForm();
  dialogVisible.value = true;
}

async function openEdit(id: number) {
  error.value = '';
  try {
    const item = await peopleApi.get(id);
    Object.assign(form, item);
    birthDate.value = toDate(item.birthDate);
    dialogVisible.value = true;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load the person.';
  }
}

async function save() {
  error.value = '';
  form.birthDate = toDateOnlyString(birthDate.value);

  try {
    if (form.id > 0) {
      await peopleApi.update({ ...form });
      toast.add({ severity: 'success', summary: 'People', detail: 'Person updated successfully.', life: 3000 });
    } else {
      await peopleApi.create({ ...form });
      toast.add({ severity: 'success', summary: 'People', detail: 'Person created successfully.', life: 3000 });
    }

    touchPeopleCatalog();
    dialogVisible.value = false;
    await loadData();
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Save failed.';
    toast.add({ severity: 'error', summary: 'People', detail: error.value, life: 5000 });
  }
}

async function remove(id: number) {
  confirm.require({
    message: 'Delete this person?',
    header: 'Confirmation',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Delete',
    rejectLabel: 'Cancel',
    acceptClass: 'p-button-danger',
    accept: async () => {
      error.value = '';
      try {
        await peopleApi.remove(id);
        touchPeopleCatalog();
        toast.add({ severity: 'success', summary: 'People', detail: 'Person deleted successfully.', life: 3000 });
        await loadData();
      } catch (err) {
        error.value = err instanceof Error ? err.message : 'Delete failed.';
        toast.add({ severity: 'error', summary: 'People', detail: error.value, life: 5000 });
      }
    }
  });
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

function onSearch() {
  page.value = 1;
  void loadData();
}

function onFilter() {
  page.value = 1;
  void loadData();
}

defineExpose({
  openCreate
});

onMounted(async () => {
  await loadCountries();
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
      <Column field="id" header="ID" sortable sortField="Id" filterField="Id" :showFilterMatchModes="false" :showAddButton="false" :filterMenuStyle="{ width: '12rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="number" inputmode="numeric" placeholder="ID" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="firstName" header="First name" sortable sortField="FirstName" filterField="FirstName" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by first name" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="lastName" header="Last name" sortable sortField="LastName" filterField="LastName" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by last name" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="email" header="Email" sortable sortField="Email" filterField="Email" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by email" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="lastNameTranscription" header="Last name transcription" sortable sortField="LastNameTranscription" filterField="LastNameTranscription" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by last name transcription" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="firstNameTranscription" header="First name transcription" sortable sortField="FirstNameTranscription" filterField="FirstNameTranscription" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by first name transcription" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="countryName" header="Country" sortable sortField="CountryName" filterField="CountryCode" :showFilterMatchModes="false" :filterMenuStyle="{ width: '14rem' }">
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
      <Column field="birthDate" header="Birth date" sortable>
        <template #body="{ data }">
          {{ formatDateOnly(data.birthDate) }}
        </template>
      </Column>
      <Column field="registrationsCount" header="Registrations" sortable sortField="RegistrationsCount" />
      <Column header="" style="width: 10rem">
        <template #body="{ data }">
          <div class="inline-actions">
            <Button icon="pi pi-pencil" text rounded @click="openEdit(data.id)" />
            <Button icon="pi pi-trash" text rounded severity="danger" @click="remove(data.id)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="dialogVisible" modal :style="{ width: '78rem' }" :header="form.id ? 'Edit person' : 'New person'">
      <div class="field-grid field-grid-person">
        <div class="field"><label>First name</label><InputText v-model="form.firstName" /></div>
        <div class="field"><label>Last name</label><InputText v-model="form.lastName" /></div>
        <div class="field"><label>First name transcription</label><InputText v-model="form.firstNameTranscription" /></div>
        <div class="field"><label>Last name transcription</label><InputText v-model="form.lastNameTranscription" /></div>
        <div class="field"><label>Address</label><InputText v-model="form.addressLine" /></div>
        <div class="field"><label>Postal code</label><InputText v-model="form.postalCode" /></div>
        <div class="field"><label>City</label><InputText v-model="form.city" /></div>
        <div class="field"><label>Address country</label><InputText v-model="form.addressCountry" /></div>
        <div class="field"><label>Email</label><InputText v-model="form.email" /></div>
        <div class="field"><label>Phone</label><InputText v-model="form.contactPhone" /></div>
        <div class="field"><label>Birth date</label><DatePicker v-model="birthDate" date-format="dd.mm.yy" show-icon fluid /></div>
        <div class="field"><label>Document number</label><InputText v-model="form.documentNumber" /></div>
        <div class="field">
          <label>Person country</label>
          <Select
            v-model="form.countryCode"
            :options="countryOptions"
            option-label="label"
            option-value="value"
            placeholder="Select country"
            filter
          />
        </div>
      </div>
      <template #footer>
        <Button label="Cancel" text @click="dialogVisible = false" />
        <Button label="Save" @click="save" />
      </template>
    </Dialog>
  </div>
</template>
