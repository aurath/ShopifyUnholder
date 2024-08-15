using ShopifyUnhold.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifyUnhold.Exceptions;

public class UserErrorsException(string message, IEnumerable<UserError> errors) : Exception(message)
{
    public IEnumerable<UserError> Errors { get; } = errors;
}
