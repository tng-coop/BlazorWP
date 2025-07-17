export function get() {
  const val = localStorage.getItem('hostInWp');
  if (val === 'true') return true;
  if (val === 'false') return false;
  return null;
}

export function set(value) {
  localStorage.setItem('hostInWp', value.toString());
}
