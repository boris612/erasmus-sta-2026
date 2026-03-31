import { deleteJson, getFile, getJson, postJson, putJson } from './http';
import type {
  EventDto,
  IdName,
  ItemsResponse,
  PageState,
  PersonDto,
  RegistrationDto,
  RegistrationUpsertDto,
  SportDto
} from './types';

function toQuery(pageState: PageState) {
  return {
    page: pageState.page,
    pageSize: pageState.pageSize,
    sort: pageState.sort,
    sortOrder: pageState.sortOrder,
    filters: pageState.filters
  };
}

export const sportsApi = {
  list: (pageState: PageState) => getJson<ItemsResponse<SportDto>>('/Sports', toQuery(pageState)),
  get: (id: number) => getJson<SportDto>(`/Sports/${id}`),
  create: (payload: SportDto) => postJson<SportDto, SportDto>('/Sports', payload),
  update: (payload: SportDto) => putJson(`/Sports/${payload.id}`, payload),
  remove: (id: number) => deleteJson(`/Sports/${id}`)
};

export const eventsApi = {
  list: (pageState: PageState) => getJson<ItemsResponse<EventDto>>('/Events', toQuery(pageState)),
  get: (id: number) => getJson<EventDto>(`/Events/${id}`),
  create: (payload: EventDto) => postJson<EventDto, EventDto>('/Events', payload),
  update: (payload: EventDto) => putJson(`/Events/${payload.id}`, payload),
  remove: (id: number) => deleteJson(`/Events/${id}`),
  downloadRegistrationsExcel: (id: number) => getFile(`/Events/${id}/RegistrationsExcel`)
};

export const peopleApi = {
  list: (pageState: PageState) => getJson<ItemsResponse<PersonDto>>('/People', toQuery(pageState)),
  get: (id: number) => getJson<PersonDto>(`/People/${id}`),
  create: (payload: PersonDto) => postJson<PersonDto, PersonDto>('/People', payload),
  update: (payload: PersonDto) => putJson(`/People/${payload.id}`, payload),
  remove: (id: number) => deleteJson(`/People/${id}`)
};

export const registrationsApi = {
  list: (pageState: PageState) => getJson<ItemsResponse<RegistrationDto>>('/Registrations', toQuery(pageState)),
  get: (id: number) => getJson<RegistrationDto>(`/Registrations/${id}`),
  create: (payload: RegistrationUpsertDto) => postJson<RegistrationDto, RegistrationUpsertDto>('/Registrations', payload),
  update: (payload: RegistrationUpsertDto) => putJson(`/Registrations/${payload.id}`, payload),
  remove: (id: number) => deleteJson(`/Registrations/${id}`),
  downloadCertificate: (id: number) => getFile(`/Registrations/${id}/Certificate`)
};

export const lookupApi = {
  countries: (text?: string) => getJson<Array<IdName<string>>>('/Lookup/Countries', { text }),
  people: (text?: string, countryCode?: string) =>
    getJson<Array<IdName<number>>>('/Lookup/People', { text, countryCode })
};
