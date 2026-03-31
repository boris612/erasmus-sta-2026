<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable, { type DataTablePageEvent, type DataTableSortEvent } from 'primevue/datatable';
import DatePicker from 'primevue/datepicker';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import { eventsApi } from '../api/eventsApi';
import { eventsCatalogVersion, touchEventsCatalog } from '../state/catalogState';
import { openRegistrationCreateForEvent } from '../state/uiState';
import type { EventDto } from '../api/types';
import { formatDateOnly, toDate, toDateOnlyString } from '../utils/dates';

const rows = ref<EventDto[]>([]);
const totalRecords = ref(0);
const loading = ref(false);
const error = ref('');
const dialogVisible = ref(false);
const page = ref(1);
const pageSize = ref(10);
const sort = ref('EventDate');
const sortOrder = ref<1 | -1>(1);
const eventDate = ref<Date | null>(null);
const confirm = useConfirm();
const toast = useToast();

const form = reactive<EventDto>({
  id: 0,
  name: '',
  eventDate: '',
  registrationsCount: 0
});

const filters = ref({
  Name: {
    operator: 'and',
    constraints: [{ value: null as string | null, matchMode: 'contains' }]
  },
  EventDate: {
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

function toFilterDate(value: unknown) {
  if (!value) {
    return '';
  }

  if (value instanceof Date && !Number.isNaN(value.getTime())) {
    return value.toISOString().slice(0, 10);
  }

  return typeof value === 'string' ? value.trim() : '';
}

function buildFilters() {
  const result: string[] = [];

  result.push(
    ...filters.value.Name.constraints
      .filter((constraint) => (constraint.value ?? '').trim() !== '')
      .map((constraint) => `Name${toSieveOperator(constraint.matchMode)}${escapeFilterValue((constraint.value ?? '').trim())}`)
  );

  result.push(
    ...filters.value.EventDate.constraints
      .map((constraint) => toFilterDate(constraint.value))
      .filter((value) => value !== '')
      .map((value) => `EventDate==${value}`)
  );

  return result.length > 0 ? result.join(',') : undefined;
}

async function loadData() {
  loading.value = true;
  error.value = '';
  try {
    const response = await eventsApi.list({
      page: page.value,
      pageSize: pageSize.value,
      sort: sort.value,
      sortOrder: sortOrder.value,
      filters: buildFilters()
    });
    rows.value = response.data ?? [];
    totalRecords.value = response.count;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load events.';
  } finally {
    loading.value = false;
  }
}

function resetForm() {
  form.id = 0;
  form.name = '';
  form.eventDate = '';
  form.registrationsCount = 0;
  eventDate.value = null;
}

function openCreate() {
  error.value = '';
  resetForm();
  dialogVisible.value = true;
}

function openRegistrationCreate(id: number) {
  openRegistrationCreateForEvent(id);
}

async function openEdit(id: number) {
  error.value = '';
  try {
    const item = await eventsApi.get(id);
    form.id = item.id;
    form.name = item.name;
    form.eventDate = item.eventDate;
    eventDate.value = toDate(item.eventDate);
    dialogVisible.value = true;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load the event.';
  }
}

async function downloadRegistrationsExcel(id: number) {
  error.value = '';

  try {
    const file = await eventsApi.downloadRegistrationsExcel(id);
    const url = URL.createObjectURL(file.blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = file.fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to download the registrations Excel file.';
    toast.add({ severity: 'error', summary: 'Events', detail: error.value, life: 5000 });
  }
}

async function save() {
  error.value = '';
  form.eventDate = toDateOnlyString(eventDate.value);
  try {
    if (form.id > 0) {
      await eventsApi.update({ ...form });
      toast.add({ severity: 'success', summary: 'Events', detail: 'Event updated successfully.', life: 3000 });
    } else {
      await eventsApi.create({ ...form });
      toast.add({ severity: 'success', summary: 'Events', detail: 'Event created successfully.', life: 3000 });
    }

    touchEventsCatalog();
    dialogVisible.value = false;
    await loadData();
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Save failed.';
    toast.add({ severity: 'error', summary: 'Events', detail: error.value, life: 5000 });
  }
}

async function remove(id: number) {
  confirm.require({
    message: 'Delete this event?',
    header: 'Confirmation',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Delete',
    rejectLabel: 'Cancel',
    acceptClass: 'p-button-danger',
    accept: async () => {
      error.value = '';
      try {
        await eventsApi.remove(id);
        touchEventsCatalog();
        toast.add({ severity: 'success', summary: 'Events', detail: 'Event deleted successfully.', life: 3000 });
        await loadData();
      } catch (err) {
        error.value = err instanceof Error ? err.message : 'Delete failed.';
        toast.add({ severity: 'error', summary: 'Events', detail: error.value, life: 5000 });
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

function onFilter() {
  page.value = 1;
  void loadData();
}

defineExpose({
  openCreate
});

onMounted(() => {
  void loadData();
});

watch(eventsCatalogVersion, () => {
  void loadData();
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
      <Column field="name" header="Name" sortable sortField="Name" filterField="Name" :filterMenuStyle="{ width: '14rem' }">
        <template #filter="{ filterModel, filterCallback }">
          <InputText v-model="filterModel.value" type="text" placeholder="Filter by name" @keydown.enter.prevent="filterCallback()" />
        </template>
      </Column>
      <Column field="eventDate" header="Date" sortable sortField="EventDate" filterField="EventDate" :showFilterMatchModes="false" :filterMenuStyle="{ width: '14rem' }">
        <template #body="{ data }">
          {{ formatDateOnly(data.eventDate) }}
        </template>
        <template #filter="{ filterModel, filterCallback }">
          <DatePicker v-model="filterModel.value" date-format="yy-mm-dd" show-icon fluid @date-select="filterCallback()" />
        </template>
      </Column>
      <Column field="registrationsCount" header="Registrations" sortable sortField="RegistrationsCount">
        <template #body="{ data }">
          <Button
            v-if="(data.registrationsCount ?? 0) > 0"
            :label="String(data.registrationsCount ?? 0)"
            link
            @click="downloadRegistrationsExcel(data.id)"
          />
          <span v-else>{{ data.registrationsCount ?? 0 }}</span>
        </template>
      </Column>
      <Column header="" style="width: 16rem">
        <template #body="{ data }">
          <div class="inline-actions">
            <Button label="New registration" link @click="openRegistrationCreate(data.id)" />
            <Button icon="pi pi-pencil" text rounded @click="openEdit(data.id)" />
            <Button icon="pi pi-trash" text rounded severity="danger" @click="remove(data.id)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="dialogVisible" modal :style="{ width: '48rem' }" :header="form.id ? 'Edit event' : 'New event'">
      <div class="field-grid">
        <div class="field">
          <label for="event-name">Name</label>
          <InputText id="event-name" v-model="form.name" />
        </div>
        <div class="field">
          <label for="event-date">Date</label>
          <DatePicker id="event-date" v-model="eventDate" date-format="dd.mm.yy" show-icon fluid />
        </div>
      </div>
      <template #footer>
        <Button label="Cancel" text @click="dialogVisible = false" />
        <Button label="Save" @click="save" />
      </template>
    </Dialog>
  </div>
</template>
