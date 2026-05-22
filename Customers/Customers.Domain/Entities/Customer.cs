using Customers.Domain.Common;
using Customers.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Customers.Domain.Entities
{
    public class Customer : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string? CustomerName { get; private set; }
        public string? CustomerType { get; private set; }

        public string? Phone { get; private set; }
        public string? Email { get; private set; }

        public string? GstNumber { get; private set; }
        public decimal? CreditLimit { get; private set; }

        public Address? BillingAddress { get; private set; }
        public Address? ShippingAddress { get; private set; }

        public string? Status { get; private set; } = string.Empty;

        // EF Core
        private Customer() { Status = null!; }

        public Customer(
            string customerName,
            string customerType,
            string phone,
            string? email,
            string? gstNumber,
            decimal creditLimit,
            Address billingAddress,
            Address? shippingAddress,
            string customerStatus,
            string createdBy)
        {
            
            CustomerName = customerName;
            CustomerType = customerType;
            Phone = phone;
            Email = email;
            GstNumber = gstNumber;
            CreditLimit = creditLimit;
            BillingAddress = billingAddress;
            ShippingAddress = shippingAddress;
            Status = customerStatus;
            CreatedBy = createdBy;
            CreatedOn = DateTime.UtcNow;
        }

        public void Update(
            string customerName,
            string customerType,
            string phone,
            string? email,
            string? gstNumber,
            decimal? creditLimit,
            Address billingAddress,
            Address? shippingAddress,
            string? status)
        {
            CustomerName = customerName;
            CustomerType = customerType;
            Phone = phone;
            Email = email;
            GstNumber = gstNumber;
            CreditLimit = creditLimit;
            Status = status;
            ModifiedOn = DateTime.UtcNow;

            if (BillingAddress == null)
                BillingAddress = billingAddress;
            else
                BillingAddress.UpdateAddressLine(billingAddress.AddressLine);

            if (shippingAddress == null)
                ShippingAddress = null;
            else if (ShippingAddress == null)
                ShippingAddress = shippingAddress;
            else
                ShippingAddress.UpdateAddressLine(shippingAddress.AddressLine);
        }

        public void UpdateStatus(string status)
        {
            Status = status;
            ModifiedOn = DateTime.UtcNow;
        }
    }

    public class Address
    {
        public string AddressLine { get; private set; }

        private Address() { AddressLine = null!; }

        public Address(string address)
        {
            AddressLine = address;
        }

        public void UpdateAddressLine(string newLine)
        {
            AddressLine = newLine;
        }
    }
}
