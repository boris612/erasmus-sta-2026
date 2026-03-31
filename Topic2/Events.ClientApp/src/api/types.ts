export interface ItemsResponse<T> {
  data: T[] | null;
  count: number;
}

export interface IdName<T> {
  id: T;
  name: string;
  description?: string | null;
}

export interface SportDto {
  id: number;
  name: string;
}

export interface EventDto {
  id: number;
  name: string;
  eventDate: string;
  registrationsCount: number;
}

export interface PersonDto {
  id: number;
  firstName: string;
  lastName: string;
  firstNameTranscription: string;
  lastNameTranscription: string;
  addressLine: string;
  postalCode: string;
  city: string;
  addressCountry: string;
  email: string;
  contactPhone: string;
  birthDate: string;
  documentNumber: string;
  countryCode: string;
  countryName: string;
  fullNameTranscription: string;
  registrationsCount: number;
}

export interface RegistrationDto {
  id: number;
  eventId: number;
  personId: number;
  sportId: number;
  registeredAt: string | null;
  personName: string;
  personTranscription: string;
  personFirstNameTranscription: string;
  personLastNameTranscription: string;
  countryCode: string;
  countryName: string;
  sportName: string;
}

export interface RegistrationUpsertDto {
  id: number;
  eventId: number;
  personId: number;
  sportId: number;
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  errorCodes?: Record<string, string[]>;
}

export interface PageState {
  page: number;
  pageSize: number;
  sort?: string;
  sortOrder?: 1 | -1;
  filters?: string;
}
