import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface Role {
  id: string;
  name: string;
  description?: string;
  permissions: string[];
  isSystem?: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface Permission {
  id: string;
  name: string;
  resource: string;
  action: string;
  description?: string;
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
  permissions: string[];
}

export interface UpdateRoleRequest {
  name?: string;
  description?: string;
  permissions?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  constructor(private apiService: ApiService) {}

  getAllRoles(): Observable<Role[]> {
    return this.apiService.get<Role[]>('/api/roles');
  }

  getRoleById(id: string): Observable<Role> {
    return this.apiService.get<Role>(`/api/roles/${id}`);
  }

  createRole(roleData: CreateRoleRequest): Observable<Role> {
    return this.apiService.post<Role>('/api/roles', roleData);
  }

  updateRole(id: string, roleData: UpdateRoleRequest): Observable<Role> {
    return this.apiService.put<Role>(`/api/roles/${id}`, roleData);
  }

  deleteRole(id: string): Observable<any> {
    return this.apiService.delete(`/api/roles/${id}`);
  }

  getAllPermissions(): Observable<Permission[]> {
    return this.apiService.get<Permission[]>('/api/permissions');
  }

  getPermissionsByResource(resource: string): Observable<Permission[]> {
    return this.apiService.get<Permission[]>(`/api/permissions?resource=${resource}`);
  }

  assignPermissionToRole(roleId: string, permissionId: string): Observable<Role> {
    return this.apiService.post<Role>(`/api/roles/${roleId}/permissions`, { permissionId });
  }

  removePermissionFromRole(roleId: string, permissionId: string): Observable<Role> {
    return this.apiService.delete<Role>(`/api/roles/${roleId}/permissions/${permissionId}`);
  }

  getUsersByRole(roleId: string): Observable<any[]> {
    return this.apiService.get<any[]>(`/api/roles/${roleId}/users`);
  }
}