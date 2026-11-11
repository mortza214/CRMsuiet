export interface RoleSummary {
  id: string;
  name: string;
  displayName: string;
}

export interface AuthUser {
  id: string;
  username: string;
  fullName: string;
  mobile: string;
  email?: string | null;
  isActive: boolean;
  roles: RoleSummary[];
  permissions: string[];
}

export interface AuthResult {
  accessToken: string;
  accessTokenExpiresAt: string;
  accessTokenExpiresAtJalali: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  refreshTokenExpiresAtJalali: string;
  user: AuthUser;
}

export interface ApiEnvelope<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: string[];
  traceId: string;
}
