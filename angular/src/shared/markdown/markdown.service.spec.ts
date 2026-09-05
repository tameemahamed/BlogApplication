import { MarkdownService } from './markdown.service';

describe('MarkdownService', () => {
    let service: MarkdownService;

    beforeEach(() => {
        service = new MarkdownService();
    });

    it('should render basic markdown', () => {
        const html = service.render('# Hello\n\nWorld');
        expect(html).toContain('<h1>Hello</h1>');
        expect(html).toContain('<p>World</p>');
    });

    it('should return an empty string for null or undefined input', () => {
        expect(service.render(null)).toBe('');
        expect(service.render(undefined)).toBe('');
        expect(service.render('')).toBe('');
    });

    it('should escape raw html embedded in the source', () => {
        const html = service.render('<script>alert(1)</script>');
        expect(html).not.toContain('<script');
        expect(html).toContain('alert(1)'); // content survives as harmless escaped text
    });

    it('should escape inline event handlers in raw html', () => {
        const html = service.render('<img src=x onerror=alert(1)>');
        expect(html).not.toContain('<img');
        expect(html).toContain('onerror=alert(1)'); // survives as harmless escaped text
    });

    it('should never emit a javascript href', () => {
        const html = service.render('[click me](javascript:alert(1))');
        expect(html).not.toContain('href="javascript:');
        expect(html).not.toContain("href='javascript:");
    });

    it('should strip dangerous content that reaches the sanitizer', () => {
        const html = service.render('<iframe src="https://evil.example"></iframe>');
        expect(html).not.toContain('<iframe');
    });

    it('should render lists and blockquotes', () => {
        const html = service.render('- a\n- b\n\n> quoted');
        expect(html).toContain('<li>a</li>');
        expect(html).toContain('<blockquote>');
    });
});
