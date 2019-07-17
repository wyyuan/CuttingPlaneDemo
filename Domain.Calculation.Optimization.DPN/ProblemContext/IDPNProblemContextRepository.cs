using Domain.Base.Demand.Reservation;
using System.Collections.Generic;

namespace Domain.Calculation.Optimization.DPN
{
    public interface IDPNProblemContextRepository : IProblemContextRepository
    {
        void Clean(string id);
        CustomerArrivalChain GetArrivalChain(string id);
        void Save(string id, CustomerArrivalChain pal);
    }
}