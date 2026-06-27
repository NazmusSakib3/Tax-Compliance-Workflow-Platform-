export interface LoginRequest {
  email: string;
  password: string;
  mfaCode?: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresUtc: string;
  userId: string;
  email: string;
  displayName: string;
  organizationId?: string;
  roles: string[];
  requiresMfa?: boolean;
}

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  expiresUtc: string;
  userId: string;
  email: string;
  displayName: string;
  organizationId?: string;
  roles: string[];
}

export interface AuthenticatedUser {
  userId: string;
  email: string;
  displayName: string;
  organizationId?: string;
  roles: string[];
  isMfaEnabled: boolean;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface MfaSetupResponse {
  sharedKey: string;
  authenticatorUri: string;
}

export interface MfaVerifyRequest {
  code: string;
}
