'use client';
export function getToken(): string | null { return typeof window !== 'undefined' ? localStorage.getItem('orchestai_token') : null; }
export function setToken(token: string) { localStorage.setItem('orchestai_token', token); }
export function clearToken() { localStorage.removeItem('orchestai_token'); }
export function isAuthenticated(): boolean { return !!getToken(); }
