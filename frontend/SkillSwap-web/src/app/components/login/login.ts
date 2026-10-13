import {
  Component,
  ChangeDetectionStrategy,
  OnInit,
  AfterViewInit,
  OnDestroy,
  ElementRef,
  ViewChild,
  NgZone,
  PLATFORM_ID,
  Inject,
  signal
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth';
import { Router } from '@angular/router';

// Partícula de la red neuronal del fondo
interface NetParticle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  r: number;
}

// Partícula que orbita alrededor de la lámpara cuando se enciende
interface Orbiter {
  angle: number;
  radius: number;
  speed: number;
  size: number;
}

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent implements OnInit, AfterViewInit, OnDestroy {

  // Referencia al <canvas> del fondo animado
  @ViewChild('neuralCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  // Control de la lámpara
  lamparaEncendida = false;

  isLoginMode = true;

  loginEmail = '';
  loginPassword = '';

  regUsername = '';
  regEmail = '';
  regPassword = '';
  regFirstName = '';
  regLastName = '';
  regBio = '';

  // Los mensajes se asignan dentro de respuestas HTTP (asíncronas): signals para refrescar la vista
  errorMessage = signal('');
  successMessage = signal('');

  // --- Estado interno de la animación del fondo ---
  private ctx: CanvasRenderingContext2D | null = null;
  private animationId = 0;
  private width = 0;
  private height = 0;
  private particles: NetParticle[] = [];
  private orbiters: Orbiter[] = [];
  private mouseX = -1000;
  private mouseY = -1000;
  private lampX = 0;   // centro de la bombilla (coordenadas del canvas)
  private lampY = 0;
  private glow = 0;    // 0 = apagado, 1 = encendido (se suaviza en cada frame)

  // Listeners guardados para poder quitarlos al destruir el componente
  private onResize = () => this.resize();
  private onMouseMove = (e: MouseEvent) => { this.mouseX = e.clientX; this.mouseY = e.clientY; };
  private onMouseLeave = () => { this.mouseX = -1000; this.mouseY = -1000; };

  constructor(
    private authService: AuthService,
    private router: Router,
    private zone: NgZone,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    // Si ya existe una sesión activa, se envía directamente al dashboard
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  ngAfterViewInit(): void {
    // El canvas y requestAnimationFrame solo existen en el navegador, no en el
    // renderizado del servidor (SSR). Sin esta guarda, SSR lanza NotYetImplemented.
    if (!isPlatformBrowser(this.platformId)) return;

    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;

    this.ctx = canvas.getContext('2d');

    this.resize();
    this.createOrbiters();

    window.addEventListener('resize', this.onResize);
    window.addEventListener('mousemove', this.onMouseMove);
    window.addEventListener('mouseleave', this.onMouseLeave);

    // La animación corre FUERA de Angular para no disparar detección de cambios en cada frame
    this.zone.runOutsideAngular(() => {
      this.animationId = requestAnimationFrame(this.animate);
    });
  }

  ngOnDestroy(): void {
    // Solo hay animación/listeners que limpiar si estuvimos en el navegador
    if (!isPlatformBrowser(this.platformId)) return;

    cancelAnimationFrame(this.animationId);
    window.removeEventListener('resize', this.onResize);
    window.removeEventListener('mousemove', this.onMouseMove);
    window.removeEventListener('mouseleave', this.onMouseLeave);
  }

  activarLampara(): void {
    this.lamparaEncendida = !this.lamparaEncendida;
    // Al encender/apagar recalculamos dónde está la bombilla en pantalla
    this.updateLampCenter();

    this.errorMessage.set('');
    this.successMessage.set('');
  }

  // =========================================================
  //  ANIMACIÓN DEL FONDO (red neuronal + órbitas de la lámpara)
  // =========================================================

  private resize(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;

    this.width = canvas.width = window.innerWidth;
    this.height = canvas.height = window.innerHeight;

    this.createParticles();
    this.updateLampCenter();
  }

  private createParticles(): void {
    // La cantidad se ajusta al tamaño de la pantalla (menos partículas en móvil)
    const count = Math.min(140, Math.floor((this.width * this.height) / 20000));
    this.particles = [];
    for (let i = 0; i < count; i++) {
      this.particles.push({
        x: Math.random() * this.width,
        y: Math.random() * this.height,
        vx: (Math.random() - 0.5) * 0.5,
        vy: (Math.random() - 0.5) * 0.5,
        r: Math.random() * 1.6 + 1
      });
    }
  }

  private createOrbiters(): void {
    this.orbiters = [];
    for (let i = 0; i < 20; i++) {
      this.orbiters.push({
        angle: Math.random() * Math.PI * 2,
        radius: 70 + Math.random() * 120,          // distancia a la bombilla
        speed: (Math.random() * 0.012 + 0.004) * (Math.random() < 0.5 ? 1 : -1),
        size: Math.random() * 2 + 1
      });
    }
  }

  // Ubica el centro de la bombilla leyendo su posición real en el DOM
  private updateLampCenter(): void {
    const canvas = this.canvasRef?.nativeElement;
    const bulb = document.querySelector('.lamp-bulb') as HTMLElement | null;
    if (!canvas || !bulb) return;

    const cRect = canvas.getBoundingClientRect();
    const bRect = bulb.getBoundingClientRect();
    this.lampX = bRect.left + bRect.width / 2 - cRect.left;
    this.lampY = bRect.top + bRect.height / 2 - cRect.top;
  }

  // Interpola entre dos valores (para pasar de gris apagado a índigo encendido)
  private lerp(a: number, b: number, t: number): number {
    return a + (b - a) * t;
  }

  private animate = (): void => {
    this.animationId = requestAnimationFrame(this.animate);
    const ctx = this.ctx;
    if (!ctx) return;

    // Suaviza la transición encendido/apagado
    const target = this.lamparaEncendida ? 1 : 0;
    this.glow += (target - this.glow) * 0.05;
    const g = this.glow;

    const speedMul = 0.45 + g * 0.9;          // más rápido al encender
    const lineDist = 130;

    // Color de las partículas: gris tenue (apagado) -> índigo brillante (encendido)
    const pr = this.lerp(120, 129, g);
    const pg = this.lerp(132, 140, g);
    const pb = this.lerp(150, 248, g);
    const pAlpha = this.lerp(0.25, 0.75, g);

    ctx.clearRect(0, 0, this.width, this.height);

    // --- LINTERNA DEL CURSOR (visible mientras la lámpara está apagada) ---
    const flashAlpha = 1 - g;
    if (flashAlpha > 0.02 && this.mouseX > -500) {
      const radius = 190;
      const grad = ctx.createRadialGradient(
        this.mouseX, this.mouseY, 0,
        this.mouseX, this.mouseY, radius
      );
      grad.addColorStop(0, `rgba(210, 225, 255, ${0.12 * flashAlpha})`);
      grad.addColorStop(1, 'rgba(210, 225, 255, 0)');
      ctx.fillStyle = grad;
      ctx.beginPath();
      ctx.arc(this.mouseX, this.mouseY, radius, 0, Math.PI * 2);
      ctx.fill();
    }

    // --- RED NEURONAL ---
    for (let i = 0; i < this.particles.length; i++) {
      const p = this.particles[i];
      p.x += p.vx * speedMul;
      p.y += p.vy * speedMul;
      if (p.x < 0 || p.x > this.width) p.vx *= -1;
      if (p.y < 0 || p.y > this.height) p.vy *= -1;

      // Iluminación según cercanía a la bombilla (solo con la lámpara encendida)
      let illum = 0;
      if (g > 0.02) {
        const ldx = p.x - this.lampX;
        const ldy = p.y - this.lampY;
        const ldist = Math.sqrt(ldx * ldx + ldy * ldy);
        const lampRadius = 300;
        if (ldist < lampRadius) {
          illum = (1 - ldist / lampRadius) * g;
        }
      }

      // Mezcla el color base con dorado según cuánto la ilumine la lámpara
      const cr = Math.round(this.lerp(pr, 255, illum));
      const cg = Math.round(this.lerp(pg, 210, illum));
      const cb = Math.round(this.lerp(pb, 120, illum));
      const cAlpha = Math.min(pAlpha + illum * 0.5, 1);

      ctx.fillStyle = `rgba(${cr}, ${cg}, ${cb}, ${cAlpha})`;
      if (illum > 0.3) {
        ctx.shadowColor = 'rgba(255, 200, 90, 0.8)';
        ctx.shadowBlur = 6 * illum;
      }
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.r + illum * 1.2, 0, Math.PI * 2);
      ctx.fill();
      ctx.shadowBlur = 0;

      // Conexiones entre partículas cercanas
      for (let j = i + 1; j < this.particles.length; j++) {
        const q = this.particles[j];
        const dx = p.x - q.x;
        const dy = p.y - q.y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        if (dist < lineDist) {
          const a = (1 - dist / lineDist) * this.lerp(0.08, 0.22, g);
          ctx.strokeStyle = `rgba(${pr}, ${pg}, ${pb}, ${a})`;
          ctx.lineWidth = 0.7;
          ctx.beginPath();
          ctx.moveTo(p.x, p.y);
          ctx.lineTo(q.x, q.y);
          ctx.stroke();
        }
      }

      // Conexión con el mouse
      const mdx = p.x - this.mouseX;
      const mdy = p.y - this.mouseY;
      const mdist = Math.sqrt(mdx * mdx + mdy * mdy);
      if (mdist < 160) {
        const a = (1 - mdist / 160) * 0.35;
        ctx.strokeStyle = `rgba(168, 85, 247, ${a})`;
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(p.x, p.y);
        ctx.lineTo(this.mouseX, this.mouseY);
        ctx.stroke();
      }
    }

    // --- PARTÍCULAS QUE ORBITAN LA LÁMPARA (solo visibles al encender) ---
    if (g > 0.02 && (this.lampX || this.lampY)) {
      for (const o of this.orbiters) {
        o.angle += o.speed * speedMul;
        const ox = this.lampX + Math.cos(o.angle) * o.radius;
        const oy = this.lampY + Math.sin(o.angle) * o.radius * 0.85; // órbita ligeramente elíptica

        // Línea tenue desde la bombilla hacia la partícula
        ctx.strokeStyle = `rgba(255, 205, 100, ${0.10 * g})`;
        ctx.lineWidth = 0.6;
        ctx.beginPath();
        ctx.moveTo(this.lampX, this.lampY);
        ctx.lineTo(ox, oy);
        ctx.stroke();

        // Punto dorado con halo
        ctx.fillStyle = `rgba(255, 210, 120, ${0.9 * g})`;
        ctx.shadowColor = 'rgba(255, 200, 80, 0.9)';
        ctx.shadowBlur = 8 * g;
        ctx.beginPath();
        ctx.arc(ox, oy, o.size, 0, Math.PI * 2);
        ctx.fill();
        ctx.shadowBlur = 0;
      }
    }
  };

  toggleMode(loginMode: boolean): void {
    this.isLoginMode = loginMode;

    this.errorMessage.set('');
    this.successMessage.set('');
  }

  onLogin(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    // Validación del lado del cliente antes de llamar a la API
    const email = this.loginEmail.trim();
    if (!email || !this.loginPassword) {
      this.errorMessage.set('Ingresa tu correo electrónico y tu contraseña.');
      return;
    }
    // Formato de correo básico
    const emailOk = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    if (!emailOk) {
      this.errorMessage.set('El correo electrónico no tiene un formato válido.');
      return;
    }

    this.authService.login({
      email,
      password: this.loginPassword
    }).subscribe({
      next: (response) => {
        // AuthService ya guardó token, userId, username, userRole y studentId en localStorage.
        // Administrador/Moderador pueden no tener perfil de estudiante, por eso se valida el token.
        if (response && response.token) {
          this.successMessage.set('¡Inicio de sesión exitoso!');
          this.router.navigate(['/dashboard']);
        } else {
          this.errorMessage.set('Respuesta inesperada del servidor.');
        }
      },
      error: (err) => {
        // 401 -> credenciales incorrectas; 0 -> API apagada; otros -> mensaje del backend
        if (err.status === 0) {
          this.errorMessage.set('No se pudo conectar con el servidor. Verifica que la API esté encendida.');
        } else {
          this.errorMessage.set(err.error?.message || 'Correo o contraseña incorrectos.');
        }
      }
    });
  }

  onRegister(): void {
    const newUser = {
      username: this.regUsername,
      email: this.regEmail,
      password: this.regPassword,
      firstName: this.regFirstName,
      lastName: this.regLastName,
      bio: this.regBio
    };

    this.authService.register(newUser).subscribe({
      next: () => {
        // toggleMode limpia los mensajes, por eso el mensaje de éxito se asigna después
        this.toggleMode(true);
        this.loginEmail = this.regEmail;

        this.successMessage.set('¡Registro exitoso! Ya puedes iniciar sesión.');
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Error al registrar el usuario.');
      }
    });
  }
}