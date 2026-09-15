import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, Link } from 'react-router-dom';
import authApi from '../api/auth.api';
import { handleApiError } from '../utils/handleApiError';
import { isRequired, isEmail, minLength } from '../utils/validation';
import { Toast } from '../components/Toast';

const Register = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();

  const [formData, setFormData] = useState({ name: '', email: '', password: '', confirmPassword: '' });
  const [formErrors, setFormErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState(null);

  const validateForm = () => {
    const errors = {};
    if (!isRequired(formData.name)) errors.name = t('validation.required');
    if (!isRequired(formData.email)) errors.email = t('validation.required');
    else if (!isEmail(formData.email)) errors.email = t('validation.email');
    if (!isRequired(formData.password)) errors.password = t('validation.required');
    else if (!minLength(formData.password, 6)) errors.password = t('validation.minLength', { min: 6 });
    if (formData.password!== formData.confirmPassword) {
      errors.confirmPassword = t('validation.passwordMismatch');
    }
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleRegister = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    try {
      await authApi.register({
        name: formData.name,
        email: formData.email,
        password: formData.password
      });
      setToast({ type: 'success', message: t('auth.registerSuccess') });
      setTimeout(() => navigate('/login'), 1500);
    } catch (error) {
      const errRes = handleApiError(error, 'auth.errorRegister');
      if (typeof errRes === 'object') {
        // Gold: ترجمة أخطاء الحقول اذا كانت مفاتيح
        const translatedErrors = {};
        Object.keys(errRes).forEach(key => {
          const val = errRes[key];
          translatedErrors[key] = typeof val === 'string' && i18n.exists(val)? t(val) : val;
        });
        setFormErrors(translatedErrors);
      } else {
        const isTranslationKey = typeof errRes === 'string' && i18n.exists(errRes);
        const msg = isTranslationKey? t(errRes) : errRes;
        setToast({ type: 'error', message: msg || t('auth.errorRegister') });
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-[var(--bg-primary)] p-4 text-[var(--text-main)]">
      {toast && <Toast type={toast.type} message={toast.message} onClose={() => setToast(null)} />}
      <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-md p-8 shadow-xl space-y-6">
        <div>
          <h1 className="text-2xl font-bold">{t('auth.registerTitle')}</h1>
          <p className="text-[var(--text-muted)] text-sm mt-1">{t('auth.registerSubtitle')}</p>
        </div>

        <form onSubmit={handleRegister} className="space-y-4">
          <div>
            <label className="block text-xs font-medium mb-1">{t('auth.name')}</label>
            <input type="text" value={formData.name} onChange={(e) => setFormData({...formData, name: e.target.value })} className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)] text-sm" />
            {formErrors.name && <span className="text-red-400 text-xs mt-1 block">{formErrors.name}</span>}
          </div>
          <div>
            <label className="block text-xs font-medium mb-1">{t('auth.email')}</label>
            <input type="email" value={formData.email} onChange={(e) => setFormData({...formData, email: e.target.value })} className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)] text-sm" />
            {formErrors.email && <span className="text-red-400 text-xs mt-1 block">{formErrors.email}</span>}
          </div>
          <div>
            <label className="block text-xs font-medium mb-1">{t('auth.password')}</label>
            <input type="password" value={formData.password} onChange={(e) => setFormData({...formData, password: e.target.value })} className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)] text-sm" />
            {formErrors.password && <span className="text-red-400 text-xs mt-1 block">{formErrors.password}</span>}
          </div>
          <div>
            <label className="block text-xs font-medium mb-1">{t('auth.confirmPassword')}</label>
            <input type="password" value={formData.confirmPassword} onChange={(e) => setFormData({...formData, confirmPassword: e.target.value })} className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)] text-sm" />
            {formErrors.confirmPassword && <span className="text-red-400 text-xs mt-1 block">{formErrors.confirmPassword}</span>}
          </div>
          <button type="submit" disabled={isSubmitting} className="w-full py-2.5 px-4 bg-[var(--accent-color)] text-white rounded-xl font-medium text-sm hover:opacity-90 transition-all disabled:opacity-50">
            {isSubmitting? t('common.processing') : t('auth.registerButton')}
          </button>
        </form>

        <div className="text-center text-xs text-[var(--text-muted)]">
          {t('auth.hasAccount')}{' '}
          <Link to="/login" className="text-[var(--accent-color)] hover:underline font-medium">{t('auth.loginLink')}</Link>
        </div>
      </div>
    </div>
  );
};

export default Register;
