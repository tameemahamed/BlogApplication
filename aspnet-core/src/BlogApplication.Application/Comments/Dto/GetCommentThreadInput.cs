using Abp.Application.Services.Dto;
using System;

namespace BlogApplication.Comments.Dto;

public class GetCommentThreadInput : PagedResultRequestDto
{
    public Guid PostId { get; set; }
}
