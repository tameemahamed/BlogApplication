import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * Route-level friendly 404 for the public site (prd.md E7-S5) - unknown
 * paths (and unmatched workspace paths) land here instead of a blank page.
 */
@Component({
    selector: 'public-not-found',
    templateUrl: './not-found.component.html',
    standalone: true,
    imports: [RouterLink, LocalizePipe],
})
export class NotFoundComponent {}
