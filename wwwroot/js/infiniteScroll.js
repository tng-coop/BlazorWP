window.infiniteScroll = (function () {
  let observer = null;
  let dotNetHelper = null;
  return {
    observe: function (element, dotNetObj) {
      dotNetHelper = dotNetObj;
      if (observer) observer.disconnect();
      observer = new IntersectionObserver(entries => {
        if (entries.some(e => e.isIntersecting)) {
          if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnIntersection');
          }
        }
      });
      observer.observe(element);
    },
    disconnect: function () {
      if (observer) {
        observer.disconnect();
        observer = null;
      }
      dotNetHelper = null;
    }
  };
})();
