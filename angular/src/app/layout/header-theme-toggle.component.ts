import { Component, ChangeDetectionStrategy } from '@angular/core';
import { ThemeService } from '@shared/theme/theme.service';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

@Component({
    selector: 'header-theme-toggle',
    templateUrl: './header-theme-toggle.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: true,
    imports: [LocalizePipe],
})
export class HeaderThemeToggleComponent {
    constructor(public themeService: ThemeService) {}
}
