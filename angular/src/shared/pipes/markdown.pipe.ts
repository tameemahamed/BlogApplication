import { Pipe, PipeTransform } from '@angular/core';
import { MarkdownService } from '../markdown/markdown.service';

/**
 * Renders Markdown to sanitized HTML for [innerHTML] bindings
 * (prd.md Epic 3 - one pipeline for posts, comments and replies).
 */
@Pipe({
    name: 'markdown',
    standalone: true,
    pure: true,
})
export class MarkdownPipe implements PipeTransform {
    constructor(private markdownService: MarkdownService) {}

    transform(markdown: string | null | undefined): string {
        return this.markdownService.render(markdown);
    }
}
