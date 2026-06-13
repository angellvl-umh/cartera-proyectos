export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export type PersonRole = 'Desarrollador' | 'JefeEquipo' | 'Gestor';

export interface Team {
  id: string;
  name: string;
  description: string | null;
  leadPersonId: string | null;
  leadName: string | null;
  memberCount: number;
}

export interface TeamMember {
  id: string;
  name: string;
  email: string;
  role: PersonRole;
  joinedAt: string;
}

export interface TeamDetail {
  id: string;
  name: string;
  description: string | null;
  leadPersonId: string | null;
  leadName: string | null;
  members: TeamMember[];
}

export interface Person {
  id: string;
  name: string;
  email: string;
  role: PersonRole;
}

export interface CreateTeamRequest {
  name: string;
  description?: string | null;
  leadPersonId?: string | null;
}

export interface UpdateTeamRequest {
  name: string;
  description?: string | null;
  leadPersonId?: string | null;
}
