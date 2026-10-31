export interface UserInfo {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  roles: string[];
  permissions: string[];
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: UserInfo;
}
