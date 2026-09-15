// src/utils/validation.js - مطابق 100% لـ Backend Validation
export const PATTERNS = {
  USERNAME: /^[a-zA-Z0-9_]{3,30}$/, // مطابق لـ chk_username
  EMAIL: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/, // مطابق لـ chk_email
  PASSWORD: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/, // 8+ حروف كبيرة+صغيرة+رقم+رمز
  PHONE: /^\+?[1-9]\d{1,14}$/, // E.164 مطابق لـ chk_phone
};

export const validateField = (fieldName, value, t) => {
  if (!value && fieldName !== 'phoneNumber' && fieldName !== 'newPassword') {
    return t('validation.required');
  }
  switch (fieldName) {
    case 'username':
      if (!PATTERNS.USERNAME.test(value)) return t('validation.invalidUsername');
      break;
    case 'email':
      if (!PATTERNS.EMAIL.test(value)) return t('validation.invalidEmail');
      break;
    case 'password':
    case 'newPassword':
      if (value && !PATTERNS.PASSWORD.test(value)) return t('validation.weakPassword');
      break;
    case 'phoneNumber':
      if (value && !PATTERNS.PHONE.test(value)) return t('validation.invalidPhone');
      break;
  }
  return null;
};