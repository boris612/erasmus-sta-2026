import { createAuth0 } from '@auth0/auth0-vue';

const authorizationParams: Record<string, string> = {
  redirect_uri: window.location.origin
};

if (import.meta.env.VITE_AUTH0_AUDIENCE) {
  authorizationParams.audience = import.meta.env.VITE_AUTH0_AUDIENCE;
}

if (import.meta.env.VITE_AUTH0_SCOPE) {
  authorizationParams.scope = import.meta.env.VITE_AUTH0_SCOPE;
}

export const auth0 = createAuth0({
  domain: import.meta.env.VITE_AUTH0_DOMAIN,
  clientId: import.meta.env.VITE_AUTH0_CLIENT_ID,
  authorizationParams
});
