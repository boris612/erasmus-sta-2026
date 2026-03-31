using Microsoft.AspNetCore.Authorization;

namespace Events.WebAPI;

public class Policies
{
  public static IEnumerable<KeyValuePair<string, Action<AuthorizationPolicyBuilder>>> All
  {
    get
    {
      yield return new KeyValuePair<string, Action<AuthorizationPolicyBuilder>>(nameof(ReadData), ReadData);
      yield return new KeyValuePair<string, Action<AuthorizationPolicyBuilder>>(nameof(EditData), EditData);
    }
  }

  public static Action<AuthorizationPolicyBuilder> ReadData
  {
    get
    {
      return policy => policy.RequireClaim("scope", "events:read");
    }
  }

  public static Action<AuthorizationPolicyBuilder> EditData
  {
    get
    {
      return policy => policy.RequireClaim("scope", "events:write");
    }
  }
}
