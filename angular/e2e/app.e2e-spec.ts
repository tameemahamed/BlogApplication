import { BlogApplicationTemplatePage } from './app.po';

describe('BlogApplication App', function () {
    let page: BlogApplicationTemplatePage;

    beforeEach(() => {
        page = new BlogApplicationTemplatePage();
    });

    it('should display message saying app works', () => {
        page.navigateTo();
        expect(page.getParagraphText()).toEqual('app works!');
    });
});
