export type UserRole = 'User' | 'Admin';

export interface AuthenticatedUser {
  token: string;
  expiresAt: string;
  userId: string;
  fullName: string;
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}
