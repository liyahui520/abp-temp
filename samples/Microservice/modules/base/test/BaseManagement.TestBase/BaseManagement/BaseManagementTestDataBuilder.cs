﻿using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace BaseManagement
{
    public class BaseManagementTestDataBuilder : ITransientDependency
    {
        private BaseManagementTestData _testData;
        private readonly ProductManager _productManager;

        public BaseManagementTestDataBuilder(
            BaseManagementTestData testData,
            ProductManager productManager)
        {
            _testData = testData;
            _productManager = productManager;
        }

        public void Build()
        {
            AsyncHelper.RunSync(BuildAsync);
        }

        [UnitOfWork]
        public virtual async Task BuildAsync()
        {
            await _productManager.CreateAsync(_testData.ProductCode1, _testData.ProductName1, _testData.ProductPrice1, _testData.ProductStockCount1);
            await _productManager.CreateAsync(_testData.ProductCode2, _testData.ProductName2, _testData.ProductPrice2, _testData.ProductStockCount2);
        }
    }
}