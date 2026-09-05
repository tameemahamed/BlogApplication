import { Injectable } from '@angular/core';
import MarkdownIt from 'markdown-it';
import DOMPurify from 'dompurify';

/**
 * Renders Markdown to sanitized HTML for all user-generated content
 * (posts, comments, replies - prd.md Epic 3).
 *
 * Security model (defense in depth):
 * - markdown-it runs with html:false, so raw HTML embedded in the source is
 *   escaped and never rendered;
 * - the rendered output is sanitized with DOMPurify before it reaches
 *   [innerHTML], neutralizing anything that slips through other channels;
 * - only raw Markdown is ever stored server-side (prd.md E3-S5).
 */
@Injectable({
    providedIn: 'root',
})
export class MarkdownService {
    private readonly parser: MarkdownIt;

    constructor() {
        this.parser = new MarkdownIt({
            html: false,
            linkify: true,
            breaks: true,
        });
    }

    render(markdown: string | null | undefined): string {
        if (!markdown) {
            return '';
        }

        return DOMPurify.sanitize(this.parser.render(markdown));
    }
}
