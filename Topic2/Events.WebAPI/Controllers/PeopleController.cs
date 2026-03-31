using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Controllers.Generic;

namespace Events.WebAPI.Controllers;

public class PeopleController : CrudController<PersonDTO, int>
{
}
