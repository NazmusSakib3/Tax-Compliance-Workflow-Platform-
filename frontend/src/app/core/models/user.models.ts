export interface UserListItem {
  userId: string;
  email: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
}

export interface CreateUserRequest {
  email: string;
  displayName: string;
  password: string;
  role: string;
}

export interface UpdateUserRequest {
  displayName: string;
  role: string;
  isActive: boolean;
}

export const USER_ROLES = ['Admin', 'ComplianceManager', 'Contributor', 'Viewer'] as const;
