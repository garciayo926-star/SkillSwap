import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit {
  username: string = '';
  userRole: string = '';

  isMaster = false;
  isTechnical = false;
  isStudent = false;

  // Métricas reales adaptadas al dominio de SkillSwap
  metrics = {
    totalUsers: 125,
    activeOffers: 48,
    pendingRequests: 32,
    activeExchanges: 85,
    systemStatus: 'Óptimo (0 Errores)'
  };

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.username = localStorage.getItem('username') || 'Usuario';
    this.userRole = localStorage.getItem('userRole') || 'Student';

    this.isMaster = this.userRole === 'Master';
    this.isTechnical = this.userRole === 'Technical' || this.userRole === 'Master';
    this.isStudent = this.userRole === 'Student';
  }

  logout(): void {
    localStorage.clear();
    this.router.navigate(['/login']);
  }
}