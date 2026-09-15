import axiosInstance from './axiosInstance';

export const authApi = {
  register: (data) => axiosInstance.post('/auth/register', data),
  login: (data) => axiosInstance.post('/auth/login', data),
  refreshToken: (data) => axiosInstance.post('/auth/refresh-token', data),
  revokeToken: (data) => axiosInstance.post('/auth/revoke-token', data),
  logout: () => axiosInstance.post('/auth/logout'),
  me: () => axiosInstance.get('/auth/me'),
  getMe: () => axiosInstance.get('/auth/me'),
};

export default authApi;
