import axiosInstance from './axiosInstance';

export const permissionsApi = {
  getAll: (params) => axiosInstance.get('/permissions', { params }),
  getById: (id) => axiosInstance.get(/permissions/),
  create: (data) => axiosInstance.post('/permissions', data),
};

export default permissionsApi;
