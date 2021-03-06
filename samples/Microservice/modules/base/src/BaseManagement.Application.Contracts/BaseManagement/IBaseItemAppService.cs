using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BaseManagement
{
    public interface IBaseItemAppService :
            ICrudAppService< //Defines CRUD methods
                BaseItemDto, //Used to show 
                Guid, //Primary key of the  entity
                BaseItemPagedRequestDto, //Used for paging/sorting on getting a list of books
                CreateUpdateBaseItemDto, //Used to create a new 
                CreateUpdateBaseItemDto> //Used to update a 
    {
       Task<List<ViewTree>>  GetViewTrees(Guid? id);
    }
}