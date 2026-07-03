using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;

namespace STS2Mobile.Patches;

internal static partial class ModelDbInitPatch
{
    private static bool TryLoadModelDbInitAccess(
        out Type[] types,
        out MethodInfo getIdMethod,
        out object contentById,
        out MethodInfo setItemMethod,
        out MethodInfo removeMethod,
        out MethodInfo containsMethod
    )
    {
        types = null;
        getIdMethod = null;
        contentById = null;
        setItemMethod = null;
        removeMethod = null;
        containsMethod = null;

        var modelDbType = typeof(ModelDb);

        var allSubtypesProp = modelDbType.GetProperty(
            AllAbstractModelSubtypesProperty,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
        );
        if (allSubtypesProp == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: AllAbstractModelSubtypes property missing");
            return false;
        }

        types = (Type[])allSubtypesProp.GetValue(null);
        if (types == null || types.Length == 0)
        {
            PatchHelper.Log("ModelDb.Init fallback: no model subtypes were exposed");
            return false;
        }

        getIdMethod = modelDbType.GetMethod(
            GetIdMethodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Type) },
            null
        );
        if (getIdMethod == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: GetId method missing");
            return false;
        }

        var contentByIdField = modelDbType.GetField(
            ContentByIdField,
            BindingFlags.NonPublic | BindingFlags.Static
        );
        if (contentByIdField == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: _contentById field missing");
            return false;
        }

        contentById = contentByIdField.GetValue(null);
        if (contentById == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: _contentById is null");
            return false;
        }

        setItemMethod = contentById.GetType().GetMethod(SetItemMethod);
        if (setItemMethod == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: _contentById.set_Item method missing");
            return false;
        }

        removeMethod = null;
        foreach (var method in contentById.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (method.Name != RemoveMethod)
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length == 1)
            {
                removeMethod = method;
                break;
            }
        }
        if (removeMethod == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: _contentById.Remove(key) method missing");
            return false;
        }

        containsMethod = modelDbType.GetMethod(
            ContainsMethodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Type) },
            null
        );
        if (containsMethod == null)
        {
            PatchHelper.Log("ModelDb.Init fallback: Contains(Type) method missing");
            return false;
        }

        return true;
    }
}
