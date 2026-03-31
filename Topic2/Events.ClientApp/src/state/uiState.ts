import { ref } from 'vue';

export const activeTab = ref('sports');
export const pendingRegistrationEventId = ref<number | null>(null);

export function openRegistrationCreateForEvent(eventId: number) {
  pendingRegistrationEventId.value = eventId;
  activeTab.value = 'registrations';
}

export function consumePendingRegistrationEventId() {
  const eventId = pendingRegistrationEventId.value;
  pendingRegistrationEventId.value = null;
  return eventId;
}
