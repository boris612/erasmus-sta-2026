import { ref } from 'vue';

export const eventsCatalogVersion = ref(0);
export const sportsCatalogVersion = ref(0);
export const peopleCatalogVersion = ref(0);

export function touchEventsCatalog() {
  eventsCatalogVersion.value += 1;
}

export function touchSportsCatalog() {
  sportsCatalogVersion.value += 1;
}

export function touchPeopleCatalog() {
  peopleCatalogVersion.value += 1;
}
