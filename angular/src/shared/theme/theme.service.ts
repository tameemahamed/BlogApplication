import { Injectable } from '@angular/core';

export type Theme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'abp.blog.theme';

/**
 * Light/dark theme switcher (prd.md §6.3). The initial theme is applied
 * before first paint by an inline script in index.html; this service keeps
 * the attribute in sync with the persisted preference afterwards.
 * System preference (`prefers-color-scheme`) is the default until the user
 * explicitly toggles (prd.md Q5).
 */
@Injectable({
    providedIn: 'root',
})
export class ThemeService {
    private _theme: Theme;

    constructor() {
        this._theme = this.readStoredTheme() ?? this.systemTheme();
        this.apply();
    }

    get theme(): Theme {
        return this._theme;
    }

    get isDark(): boolean {
        return this._theme === 'dark';
    }

    toggle(): void {
        this.set(this.isDark ? 'light' : 'dark');
    }

    set(theme: Theme): void {
        this._theme = theme;
        try {
            localStorage.setItem(THEME_STORAGE_KEY, theme);
        } catch (e) {
            // storage unavailable (private mode etc.) - theme still applies for the session
        }
        this.apply();
    }

    private apply(): void {
        document.documentElement.setAttribute('data-theme', this._theme);
    }

    private readStoredTheme(): Theme | null {
        try {
            const stored = localStorage.getItem(THEME_STORAGE_KEY);
            return stored === 'dark' || stored === 'light' ? stored : null;
        } catch (e) {
            return null;
        }
    }

    private systemTheme(): Theme {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
}
