<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import { useConfirm } from 'primevue/useconfirm';
import { useToast } from 'primevue/usetoast';
import DataTable, { type DataTablePageEvent, type DataTableSortEvent } from 'primevue/datatable';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import { sportsApi } from '../api/eventsApi';
import { touchSportsCatalog } from '../state/catalogState';
import type { SportDto } from '../api/types';

const rows = ref<SportDto[]>([]);
const totalRecords = ref(0);
const loading = ref(false);
const error = ref('');
const dialogVisible = ref(false);
const page = ref(1);
const pageSize = ref(10);
const sort = ref('Id');
const sortOrder = ref<1 | -1>(1);
const confirm = useConfirm();
const toast = useToast();

const form = reactive<SportDto>({
  id: 0,
  name: ''
});

const filters = ref({
  Name: {
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

function buildFilters() {
  const result = filters.value.Name.constraints
    .filter((constraint) => (constraint.value ?? '').trim() !== '')
    .map((constraint) => `Name${toSieveOperator(constraint.matchMode)}${escapeFilterValue((constraint.value ?? '').trim())}`);

  return result.length > 0 ? result.join(',') : undefined;
}

async function loadData() {
  loading.value = true;
  error.value = '';
  try {
    const response = await sportsApi.list({
      page: page.value,
      pageSize: pageSize.value,
      sort: sort.value,
      sortOrder: sortOrder.value,
      filters: buildFilters()
    });
    rows.value = response.data ?? [];
    totalRecords.value = response.count;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load sports.';
  } finally {
    loading.value = false;
  }
}

function resetForm() {
  form.id = 0;
  form.name = '';
}

function openCreate() {
  error.value = '';
  resetForm();
  dialogVisible.value = true;
}

async function openEdit(id: number) {
  error.value = '';
  try {
    const item = await sportsApi.get(id);
    form.id = item.id;
    form.name = item.name;
    dialogVisible.value = true;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load the sport.';
  }
}

async function save() {
  error.value = '';
  try {
    if (form.id > 0) {
      await sportsApi.update({ ...form });
      toast.add({ severity: 'success', summary: 'Sports', detail: 'Sport updated successfully.', life: 3000 });
    } else {
      await sportsApi.create({ ...form });
      toast.add({ severity: 'success', summary: 'Sports', detail: 'Sport created successfully.', life: 3000 });
    }

    touchSportsCatalog();
    dialogVisible.value = false;
    await loadData();
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Save failed.';
    toast.add({ severity: 'error', summary: 'Sports', detail: error.value, life: 5000 });
  }
}

async function remove(id: number) {
  confirm.require({
    message: 'Delete this sport?',
    header: 'Confirmation',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Delete',
    rejectLabel: 'Cancel',
    acceptClass: 'p-button-danger',
    accept: async () => {
      error.value = '';
      try {
        await sportsApi.remove(id);
        touchSportsCatalog();
        toast.add({ severity: 'success', summary: 'Sports', detail: 'Sport deleted successfully.', life: 3000 });
        await loadData();
      } catch (err) {
        error.value = err instanceof Error ? err.message : 'Delete failed.';
        toast.add({ severity: 'error', summary: 'Sports', detail: error.value, life: 5000 });
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
      <Column header="" style="width: 10rem">
        <template #body="{ data }">
          <div class="inline-actions">
            <Button icon="pi pi-pencil" text rounded @click="openEdit(data.id)" />
            <Button icon="pi pi-trash" text rounded severity="danger" @click="remove(data.id)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="dialogVisible" modal :style="{ width: '42rem' }" :header="form.id ? 'Edit sport' : 'New sport'">
      <div class="field">
        <label for="sport-name">Name</label>
        <InputText id="sport-name" v-model="form.name" />
      </div>
      <template #footer>
        <Button label="Cancel" text @click="dialogVisible = false" />
        <Button label="Save" @click="save" />
      </template>
    </Dialog>
  </div>
</template>
