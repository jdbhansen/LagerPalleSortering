const navigationEventName = 'app:navigate';

function dispatchNavigationEvent() {
  window.dispatchEvent(new Event(navigationEventName));
}

export function navigateTo(path: string, replace = false) {
  if (replace) {
    window.history.replaceState({}, '', path);
  } else {
    window.history.pushState({}, '', path);
  }

  dispatchNavigationEvent();
}

export function subscribeNavigation(handler: () => void) {
  window.addEventListener('popstate', handler);
  window.addEventListener(navigationEventName, handler);

  return () => {
    window.removeEventListener('popstate', handler);
    window.removeEventListener(navigationEventName, handler);
  };
}

