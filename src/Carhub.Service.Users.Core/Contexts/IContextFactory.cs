namespace Carhub.Service.Users.Core.Contexts;

internal interface IContextFactory
{
    IContext Create();
}