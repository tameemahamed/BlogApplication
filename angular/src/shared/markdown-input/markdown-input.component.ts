import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MarkdownPipe } from '@shared/pipes/markdown.pipe';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * Markdown textarea with a Write/Preview toggle (prd.md Epic 3), shared by
 * the comment composer, reply forms and inline comment editing. Two-way
 * [(value)] binding; used outside ngForm so no ControlValueAccessor needed.
 */
@Component({
    selector: 'markdown-input',
    templateUrl: './markdown-input.component.html',
    standalone: true,
    imports: [MarkdownPipe, LocalizePipe],
})
export class MarkdownInputComponent {
    @Input() value = '';
    @Output() valueChange = new EventEmitter<string>();
    @Input() rows = 4;
    @Input() placeholder = '';
    @Input() maxlength = 5000;

    previewMode = false;

    onInput(event: Event): void {
        this.valueChange.emit((event.target as HTMLTextAreaElement).value);
    }
}
