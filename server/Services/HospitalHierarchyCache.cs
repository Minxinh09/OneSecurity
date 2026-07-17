using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;

namespace OneSecurity.Server.Services
{
    public interface IHospitalHierarchyCache
    {
        void RefreshCache(IServiceProvider serviceProvider);
        List<int> GetDescendantIds(int hospitalId);
    }

    public class HospitalHierarchyCache : IHospitalHierarchyCache
    {
        private Dictionary<int, int?> _hospitalParents = new();
        private readonly object _lock = new();

        public void RefreshCache(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LocalAgentDbContext>();
            var all = db.Hospitals.IgnoreQueryFilters().Select(h => new { h.Id, h.ParentId }).ToList();
            
            lock (_lock)
            {
                _hospitalParents = all.ToDictionary(h => h.Id, h => h.ParentId);
            }
        }

        public List<int> GetDescendantIds(int hospitalId)
        {
            var result = new List<int>();
            lock (_lock)
            {
                void FindChildren(int parentId)
                {
                    result.Add(parentId);
                    var children = _hospitalParents.Where(kvp => kvp.Value == parentId).Select(kvp => kvp.Key);
                    foreach (var childId in children)
                    {
                        FindChildren(childId);
                    }
                }
                FindChildren(hospitalId);
            }
            return result;
        }
    }
}
