export const handleApiError = (err, fallbackKey = 'common.errorLoading') => {
  if (!err.response) {
    return { general: fallbackKey };
  }

  const { status, data } = err.response;

  // إذا أرجع الخادم أخطاء تحقق مفصلة لكل حقل (مثل 422)
  if (status === 422 || status === 400) {
    if (data?.errors) {
      return data.errors; // إرجاع أخطاء الحقول مباشرة
    }
    if (data?.message) {
      return { general: data.message };
    }
  }

  // استخدام مفاتيح ترجمة معيارية بدلاً من النصوص الإنجليزية الثابتة
  if (status === 403) {
    return { general: 'common.accessDenied' };
  }

  if (status === 404) {
    return { general: 'common.notFound' };
  }

  return { general: data?.message || fallbackKey };
};