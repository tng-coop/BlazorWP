window.wpEndpointSync = (function () {
  let dotNetHelper = null;
  function handleStorage(e) {
    if (e.key === 'wpEndpoint' && dotNetHelper) {
      dotNetHelper.invokeMethodAsync('UpdateEndpoint', e.newValue);
    }
  }
  return {
    register: function (dotnet) {
      dotNetHelper = dotnet;
      window.addEventListener('storage', handleStorage);
      dotnet.invokeMethodAsync('UpdateEndpoint', localStorage.getItem('wpEndpoint'));
    },
    unregister: function () {
      window.removeEventListener('storage', handleStorage);
      dotNetHelper = null;
    },
    set: function (value) {
      localStorage.setItem('wpEndpoint', value);
      if (dotNetHelper) {
        dotNetHelper.invokeMethodAsync('UpdateEndpoint', value);
      }
    }
  };
})();
