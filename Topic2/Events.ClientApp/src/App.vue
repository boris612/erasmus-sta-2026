<script setup lang="ts">
import { computed, ref } from 'vue';
import { useAuth0 } from '@auth0/auth0-vue';
import Button from 'primevue/button';
import ConfirmDialog from 'primevue/confirmdialog';
import Menu from 'primevue/menu';
import Tab from 'primevue/tab';
import TabList from 'primevue/tablist';
import TabPanel from 'primevue/tabpanel';
import TabPanels from 'primevue/tabpanels';
import Tabs from 'primevue/tabs';
import Toast from 'primevue/toast';
import EventsPanel from './components/EventsPanel.vue';
import PeoplePanel from './components/PeoplePanel.vue';
import RegistrationsPanel from './components/RegistrationsPanel.vue';
import SportsPanel from './components/SportsPanel.vue';
import { activeTab } from './state/uiState';

const {
  isLoading,
  isAuthenticated,
  error,
  loginWithRedirect,
  logout: auth0Logout,
  user
} = useAuth0();

const sportsPanelRef = ref<{ openCreate: () => void } | null>(null);
const eventsPanelRef = ref<{ openCreate: () => void } | null>(null);
const peoplePanelRef = ref<{ openCreate: () => void } | null>(null);
const registrationsPanelRef = ref<{ openCreate: () => void } | null>(null);
const userMenuRef = ref<{ toggle: (event: Event) => void } | null>(null);

const actionLabel = computed(() => {
  switch (activeTab.value) {
    case 'events':
      return 'New event';
    case 'people':
      return 'New person';
    case 'registrations':
      return 'New registration';
    case 'sports':
    default:
      return 'New sport';
  }
});

function openActiveCreate() {
  switch (activeTab.value) {
    case 'events':
      eventsPanelRef.value?.openCreate();
      break;
    case 'people':
      peoplePanelRef.value?.openCreate();
      break;
    case 'registrations':
      registrationsPanelRef.value?.openCreate();
      break;
    case 'sports':
    default:
      sportsPanelRef.value?.openCreate();
      break;
  }
}

function login() {
  return loginWithRedirect();
}

function logout() {
  return auth0Logout({ logoutParams: { returnTo: window.location.origin } });
}

const userMenuItems = [
  {
    label: 'Logout',
    icon: 'pi pi-sign-out',
    command: logout
  }
];

function toggleUserMenu(event: Event) {
  userMenuRef.value?.toggle(event);
}
</script>

<template>
  <div class="app-shell">
    <Toast position="top-right" />
    <ConfirmDialog />
    <Menu ref="userMenuRef" :model="userMenuItems" popup />

    <section v-if="isLoading" class="workspace auth-state">
      <div class="panel-card auth-card">Loading...</div>
    </section>

    <section v-else-if="!isAuthenticated" class="workspace auth-state">
      <div class="panel-card auth-card">
        <h1>Events</h1>
        <p v-if="error">Error: {{ error.message }}</p>
        <div class="inline-actions">
          <Button label="Login" @click="login" />
        </div>
      </div>
    </section>

    <section v-else class="workspace">
      <Tabs v-model:value="activeTab">
        <div class="tabs-header-bar">
          <TabList>
            <Tab value="sports">Sports</Tab>
            <Tab value="events">Events</Tab>
            <Tab value="people">People</Tab>
            <Tab value="registrations">Registrations</Tab>
          </TabList>
          <div class="tabs-header-center">
            <Button :label="actionLabel" icon="pi pi-plus" @click="openActiveCreate" />
          </div>
          <div class="tabs-header-actions">
            <button class="user-chip user-chip-button" type="button" @click="toggleUserMenu($event)">
              {{ user?.email || user?.name || 'Authenticated user' }}
              <span class="pi pi-angle-down" />
            </button>
          </div>
        </div>
        <TabPanels>
          <TabPanel value="sports">
            <SportsPanel ref="sportsPanelRef" />
          </TabPanel>
          <TabPanel value="events">
            <EventsPanel ref="eventsPanelRef" />
          </TabPanel>
          <TabPanel value="people">
            <PeoplePanel ref="peoplePanelRef" />
          </TabPanel>
          <TabPanel value="registrations">
            <RegistrationsPanel ref="registrationsPanelRef" />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </section>
  </div>
</template>
