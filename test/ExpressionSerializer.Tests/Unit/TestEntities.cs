using System;
using System.Collections.Generic;

namespace ExpressionSerializer.Tests.Unit
{
    public class Customer
    {
        public Guid Id {get;set;}
        public string FirstName {get;set;}
        public string LastName {get;set;}
        public virtual Company Company {get;set;}
        public virtual ICollection<Order> Orders {get;set;}
    }

    public class Order
    {
        public Guid Id {get;set;}
        public DateTime CreatedOn {get;set;}
        public decimal Total {get;set;}
        public virtual Customer Customer {get;set;}
    }

    public class Company
    {
        public Guid Id {get;set;}
        public string Name {get;set;}
        public virtual ICollection<Customer> Customers {get;set;}
    }
}