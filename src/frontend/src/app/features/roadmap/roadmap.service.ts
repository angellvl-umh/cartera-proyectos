import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RoadmapMilestoneDto {
  id: number;
  title: string;
  hitoDate: string | null;
  reached: boolean;
}

export interface RoadmapProjectDto {
  id: number;
  title: string;
  status: string;
  complexity: string;
  businessValue: number | null;
  startDate: string | null;
  endDate: string | null;
  desiredDeploymentDate: string | null;
  milestones: RoadmapMilestoneDto[];
}

export interface RoadmapTeamDto {
  teamId: number;
  teamName: string;
  projects: RoadmapProjectDto[];
}

export interface PortfolioRoadmapDto {
  year: number;
  teams: RoadmapTeamDto[];
  unassigned: RoadmapProjectDto[];
  undated: RoadmapProjectDto[];
  availableYears: number[];
}

@Injectable({ providedIn: 'root' })
export class RoadmapService {
  private readonly http = inject(HttpClient);

  getRoadmap(year: number): Observable<PortfolioRoadmapDto> {
    return this.http.get<PortfolioRoadmapDto>(`/api/portfolio/roadmap?year=${year}`);
  }
}
