using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibraryDBApi.Core;
using LibraryDBApi.Models;

namespace LibraryDBApi.Examples
{
    /// <summary>
    /// Ejemplos de uso del nuevo modelo StoredProcedureParameters con ModelPaginacion
    /// </summary>
    public class StoredProcedureUsageExample
    {
        private readonly IDataService _dataService;

        public StoredProcedureUsageExample(IDataService dataService)
        {
            _dataService = dataService;
        }

        /// <summary>
        /// Ejemplo 1: Ejecutar procedimiento sin parámetros, solo con paginación
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<UserModel>>> GetUsersWithPaginationAsync()
        {
            var parameters = new StoredProcedureParameters
            {
                ConnectionString = "Server=myServer;Database=myDB;Trusted_Connection=true;",
                ProcedureName = "GetUsers",
                ModelPaginacion = new ModelPaginacion
                {
                    PageNumber = 1,
                    PageSize = 10
                }
            };

            return await _dataService.EjecutarProcedimientoAsync<UserModel>(parameters);
        }

        /// <summary>
        /// Ejemplo 2: Ejecutar procedimiento con modelo de parámetros y filtro
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<UserModel>>> GetUsersWithFilterAsync()
        {
            var searchModel = new UserSearchModel
            {
                IsActive = true,
                DepartmentId = 5
            };

            var parameters = new StoredProcedureParameters
            {
                ConnectionString = "Server=myServer;Database=myDB;Trusted_Connection=true;",
                ProcedureName = "GetUsersByDepartment",
                Model = searchModel,
                ModelPaginacion = new ModelPaginacion
                {
                    PageNumber = 1,
                    PageSize = 20,
                    Filter = "John" // Filtro adicional de búsqueda
                }
            };

            return await _dataService.EjecutarProcedimientoAsync<UserModel>(parameters);
        }

        /// <summary>
        /// Ejemplo 3: Ejecutar procedimiento solo con filtro (sin modelo)
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<ProductModel>>> GetProductsByFilterAsync()
        {
            var parameters = new StoredProcedureParameters
            {
                ConnectionString = "Server=myServer;Database=myDB;Trusted_Connection=true;",
                ProcedureName = "GetProducts",
                ModelPaginacion = new ModelPaginacion
                {
                    Filter = "electronics" // Filtro de categoría
                }
            };

            return await _dataService.EjecutarProcedimientoAsync<ProductModel>(parameters);
        }

        /// <summary>
        /// Ejemplo 4: Usando constructor con parámetros
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<OrderModel>>> GetOrdersAsync()
        {
            var orderSearch = new OrderSearchModel
            {
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now,
                Status = "Completed"
            };

            var modelPaginacion = new ModelPaginacion(
                pageNumber: 1,
                pageSize: 50,
                filter: "priority"
            );

            var parameters = new StoredProcedureParameters(
                connectionString: "Server=myServer;Database=myDB;Trusted_Connection=true;",
                procedureName: "GetOrders",
                model: orderSearch,
                modelPaginacion: modelPaginacion
            );

            return await _dataService.EjecutarProcedimientoAsync<OrderModel>(parameters);
        }

        /// <summary>
        /// Ejemplo 5: Procedimiento simple sin paginación ni filtro
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<CategoryModel>>> GetCategoriesAsync()
        {
            var parameters = new StoredProcedureParameters
            {
                ConnectionString = "Server=myServer;Database=myDB;Trusted_Connection=true;",
                ProcedureName = "GetCategories"
            };

            return await _dataService.EjecutarProcedimientoAsync<CategoryModel>(parameters);
        }

        /// <summary>
        /// Ejemplo 6: Solo con paginación (sin filtro)
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<UserModel>>> GetUsersPagedOnlyAsync()
        {
            var parameters = new StoredProcedureParameters
            {
                ConnectionString = "Server=myServer;Database=myDB;Trusted_Connection=true;",
                ProcedureName = "GetUsers",
                ModelPaginacion = new ModelPaginacion
                {
                    PageNumber = 2,
                    PageSize = 15
                }
            };

            return await _dataService.EjecutarProcedimientoAsync<UserModel>(parameters);
        }

        /// <summary>
        /// Ejemplo 7: Solo con filtro (sin paginación)
        /// </summary>
        public async Task<StoredProcedureResult<IEnumerable<ProductModel>>> GetProductsFilteredOnlyAsync()
        {
            var parameters = new StoredProcedureParameters
            {
                ConnectionString = "Server=myServer;Database=myDB;Trusted_Connection=true;",
                ProcedureName = "GetProducts",
                ModelPaginacion = new ModelPaginacion
                {
                    Filter = "electronics"
                }
            };

            return await _dataService.EjecutarProcedimientoAsync<ProductModel>(parameters);
        }
    }

    // Modelos de ejemplo
    public class UserModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public int DepartmentId { get; set; }
    }

    public class UserSearchModel
    {
        public bool? IsActive { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class ProductModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderModel
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
    }

    public class OrderSearchModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
    }

    public class CategoryModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
} 