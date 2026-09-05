using Abp.AspNetCore.TestBase;
using Abp.Authorization.Users;
using Abp.Json;
using Abp.Web.Models;
using BlogApplication.EntityFrameworkCore;
using BlogApplication.Models.TokenAuth;
using BlogApplication.Web.Startup;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Shouldly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlogApplication.Web.Tests;

public abstract class BlogApplicationWebTestBase : AbpAspNetCoreIntegratedTestBase<Startup>
{
    protected static readonly Lazy<string> ContentRootFolder;

    static BlogApplicationWebTestBase()
    {
        ContentRootFolder = new Lazy<string>(WebContentDirectoryFinder.CalculateContentRootFolder, true);
    }

    protected override IWebHostBuilder CreateWebHostBuilder()
    {
        return base
            .CreateWebHostBuilder()
            .UseContentRoot(ContentRootFolder.Value)
            .UseSetting(WebHostDefaults.ApplicationKey, typeof(BlogApplicationWebMvcModule).Assembly.FullName);
    }

    #region Get response

    protected async Task<T> GetResponseAsObjectAsync<T>(string url,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var strResponse = await GetResponseAsStringAsync(url, expectedStatusCode);
        return JsonSerializer.Deserialize<T>(strResponse, new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    protected async Task<string> GetResponseAsStringAsync(string url,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var response = await GetResponseAsync(url, expectedStatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    protected async Task<HttpResponseMessage> GetResponseAsync(string url,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(expectedStatusCode);
        return response;
    }

    #endregion

    #region Authenticate

    /// <summary>
    /// /api/TokenAuth/Authenticate
    /// TokenAuthController
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    protected async Task AuthenticateAsync(AuthenticateModel input)
    {
        var response = await Client.PostAsync("/api/TokenAuth/Authenticate",
            new StringContent(input.ToJsonString(), Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<AjaxResponse<AuthenticateResultModel>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Result.AccessToken);

        AbpSession.UserId = result.Result.UserId;
    }

    #endregion

    #region Login

    protected void LoginAsHostAdmin()
    {
        LoginAsHost(AbpUserBase.AdminUserName);
    }

    protected void LoginAsHost(string userName)
    {
        AbpSession.TenantId = null;

        var user =
            UsingDbContext(
                context =>
                    context.Users.FirstOrDefault(u => u.TenantId == AbpSession.TenantId && u.UserName == userName));
        if (user == null)
        {
            throw new Exception("There is no user: " + userName + " for host.");
        }

        AbpSession.UserId = user.Id;
    }

    #endregion

    #region UsingDbContext

    protected void UsingDbContext(Action<BlogApplicationDbContext> action)
    {
        using (var context = IocManager.Resolve<BlogApplicationDbContext>())
        {
            action(context);
            context.SaveChanges();
        }
    }

    protected T UsingDbContext<T>(Func<BlogApplicationDbContext, T> func)
    {
        T result;

        using (var context = IocManager.Resolve<BlogApplicationDbContext>())
        {
            result = func(context);
            context.SaveChanges();
        }

        return result;
    }

    protected async Task UsingDbContextAsync(Func<BlogApplicationDbContext, Task> action)
    {
        using (var context = IocManager.Resolve<BlogApplicationDbContext>())
        {
            await action(context);
            await context.SaveChangesAsync(true);
        }
    }

    protected async Task<T> UsingDbContextAsync<T>(Func<BlogApplicationDbContext, Task<T>> func)
    {
        T result;

        using (var context = IocManager.Resolve<BlogApplicationDbContext>())
        {
            result = await func(context);
            await context.SaveChangesAsync(true);
        }

        return result;
    }

    #endregion

    #region ParseHtml

    protected IHtmlDocument ParseHtml(string htmlString)
    {
        return new HtmlParser().ParseDocument(htmlString);
    }

    #endregion
}
