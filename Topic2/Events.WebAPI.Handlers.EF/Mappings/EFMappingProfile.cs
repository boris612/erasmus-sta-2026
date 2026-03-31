using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;

namespace Events.WebAPI.Handlers.EF.Mappings;

public class EFMappingProfile : Profile
{
  public EFMappingProfile()
  {
    CreateMap<Event, EventDTO>()
      .ForMember(o => o.RegistrationsCount, opt => opt.MapFrom(src => src.Registrations.Count));
    CreateMap<EventDTO, Event>()
      .ForMember(o => o.Id, opt => opt.Ignore());

    CreateMap<Person, PersonDTO>()
      .ForMember(o => o.CountryName, opt => opt.MapFrom(src => src.CountryCodeNavigation.Name))
      .ForMember(o => o.FullNameTranscription, opt => opt.MapFrom(src => src.FirstNameTranscription + " " + src.LastNameTranscription))
      .ForMember(o => o.RegistrationsCount, opt => opt.MapFrom(src => src.Registrations.Count));
    CreateMap<PersonDTO, Person>()
      .ForMember(o => o.Id, opt => opt.Ignore());

    CreateMap<Registration, RegistrationDTO>()
      .ForMember(o => o.PersonName, opt => opt.MapFrom(src => src.Person.FirstName + " " + src.Person.LastName))
      .ForMember(o => o.PersonTranscription, opt => opt.MapFrom(src => src.Person.FirstNameTranscription + " " + src.Person.LastNameTranscription))
      .ForMember(o => o.PersonFirstNameTranscription, opt => opt.MapFrom(src => src.Person.FirstNameTranscription))
      .ForMember(o => o.PersonLastNameTranscription, opt => opt.MapFrom(src => src.Person.LastNameTranscription))
      .ForMember(o => o.CountryCode, opt => opt.MapFrom(src => src.Person.CountryCode))
      .ForMember(o => o.CountryName, opt => opt.MapFrom(src => src.Person.CountryCodeNavigation.Name))
      .ForMember(o => o.SportName, opt => opt.MapFrom(src => src.Sport.Name));
    CreateMap<RegistrationDTO, Registration>()
      .ForMember(o => o.Id, opt => opt.Ignore())
      .ForMember(o => o.RegisteredAt, opt => opt.Ignore());

    CreateMap<Sport, SportDTO>();
    CreateMap<SportDTO, Sport>()
      .ForMember(o => o.Id, opt => opt.Ignore());
  }    
}
