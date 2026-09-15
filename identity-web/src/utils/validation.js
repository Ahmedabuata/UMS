export const isRequired = (value) => {
  if (value === null || value === undefined) return false;
  if (typeof value === 'string') return value.trim().length > 0;
  if (Array.isArray(value)) return value.length > 0;
  return true;
};

export const isEmail = (value) => {
  if (!value) return false;
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
};

export const isValidPhone = (value) => {
  if (!value) return false;
  return /^\+[1-9]\d{1,14}$/.test(value);
};

// ✅ Gold Alias - Users.jsx يستخدم isValidPhoneE164
export const isValidPhoneE164 = isValidPhone;

export const minLength = (value, min) => {
  if (!value) return false;
  return String(value).trim().length >= min;
};

// Gold V1.2 - Aliases توافق مع كل الصفحات
export const required = isRequired;
export const validateRequired = isRequired;
export const isValidEmail = isEmail;
export const validateEmail = isEmail;
export const validatePhone = isValidPhone;
export const validatePhoneE164 = isValidPhoneE164;