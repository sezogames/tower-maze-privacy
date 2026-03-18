using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Services;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Backward-compatible alias for older clients that still request get_tool_states.
    /// Returns both a flat map and a per-tool list so different client versions can consume it.
    /// </summary>
    [McpForUnityTool("get_tool_states", AutoRegister = false)]
    public static class GetToolStates
    {
        public static object HandleCommand(JObject @params)
        {
            var toolDiscovery = MCPServiceLocator.ToolDiscovery;
            if (toolDiscovery == null)
            {
                return new ErrorResponse("Tool discovery service is unavailable.");
            }

            var tools = toolDiscovery
                .DiscoverAllTools()
                .OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
                .Select(tool => new
                {
                    name = tool.Name,
                    enabled = toolDiscovery.IsToolEnabled(tool.Name),
                    autoRegister = tool.AutoRegister,
                    isBuiltIn = tool.IsBuiltIn,
                    className = tool.ClassName,
                    @namespace = tool.Namespace
                })
                .ToList();

            var toolStates = tools.ToDictionary(tool => tool.name, tool => tool.enabled, StringComparer.OrdinalIgnoreCase);

            return new SuccessResponse(
                "Retrieved tool states.",
                new
                {
                    tools,
                    toolStates,
                    tool_states = toolStates,
                    enabledCount = tools.Count(tool => tool.enabled),
                    totalCount = tools.Count
                });
        }
    }
}
