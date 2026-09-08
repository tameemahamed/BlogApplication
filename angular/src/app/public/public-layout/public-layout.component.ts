import { Component, Injector, OnInit, Renderer2 } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AppComponentBase } from '@shared/app-component-base';
import { AppAuthService } from '@shared/auth/app-auth.service';
import { ThemeService } from '@shared/theme/theme.service';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * The front-of-house shell (prd.md E7): anyone can browse and read. The
 * workspace keeps its own AdminLTE shell under /app; this layout is used by
 * the public routes (home, post detail). Interactive nav is permission-aware
 * (prd.md E7-S4) and anonymous visitors get login/register entry points
 * (prd.md E7-S3/E7-S6).
 */
@Component({
    selector: 'public-layout',
    templateUrl: './public-layout.component.html',
    standalone: true,
    imports: [RouterLink, RouterOutlet, LocalizePipe],
})
export class PublicLayoutComponent extends AppComponentBase implements OnInit {
    readonly currentYear = new Date().getFullYear();

    constructor(
        injector: Injector,
        private _authService: AppAuthService,
        private _renderer: Renderer2,
        public themeService: ThemeService
    ) {
        super(injector);
    }

    ngOnInit(): void {
        // the workspace shell tags body with AdminLTE's sidebar-mini; public
        // pages must not inherit it after navigating out of /app
        this._renderer.removeClass(document.body, 'sidebar-mini');
    }

    isAuthenticated(): boolean {
        return !!this.appSession.userId;
    }

    // role-aware navigation (prd.md E7-S4) - same permissions as the sidebar
    canAuthor(): boolean {
        return this.permission.isGranted('Pages.Blog.Posts.Create');
    }

    canApprove(): boolean {
        return this.permission.isGranted('Pages.Blog.Posts.Approve');
    }

    canModerate(): boolean {
        return this.permission.isGranted('Pages.Blog.Bans.Manage');
    }

    logout(): void {
        this._authService.logout();
    }
}
