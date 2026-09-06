import {
  Building2, Boxes, CalendarRange, FileClock, Flag,
  GraduationCap, LayoutDashboard, LockKeyhole, Network, School, Shield,
  ShieldCheck, UserRound, Users, Warehouse
} from 'lucide-react'

export const NAVIGATION = [
  // 1. Dashboard وحدها - للجميع
  {
    title: 'Overview',
    icon: LayoutDashboard,
    expandedByDefault: true,
    requiredPermissions: [], // الجميع
    isStandalone: true, // علامة خاصة - تظهر بدون تغليف
    items: [
      { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, perms: [] },
    ],
  },
  // 2. HR Management إدارة منفصلة تماماً - خاصة بـ Ali@uni.com
  {
    title: 'HR Management',
    icon: UserRound,
    expandedByDefault: true,
    requiredPermissions: [
      'HR_EMPLOYEE_READ','HR_EMPLOYEE_WRITE',
      'HR_ATTENDANCE_READ','HR_ATTENDANCE_WRITE',
      'HR_RECRUITMENT_READ','HR_RECRUITMENT_WRITE',
      'HR_LEAVE_READ','HR_LEAVE_WRITE',
      'HR_CONTRACT_READ','HR_CONTRACT_WRITE',
      'HR_SALARY_READ','HR_SALARY_WRITE',
      'HR_EVALUATION_READ','HR_EVALUATION_WRITE',
      'HR_EMPLOYEE_STATUS',
      'SUPER_ADMIN'
    ],
    items: [
      { to: '/hr', label: 'HR Dashboard', icon: UserRound, perms: ['HR_EMPLOYEE_READ','HR_ATTENDANCE_READ','HR_RECRUITMENT_READ','SUPER_ADMIN'] },
      // مستقبلاً يمكنك إضافة:
      // { to: '/hr/employees', label: 'Employees', icon: Users, perms: ['HR_EMPLOYEE_READ'] },
      // { to: '/hr/attendance', label: 'Attendance', icon: FileClock, perms: ['HR_ATTENDANCE_READ'] },
    ],
  },
  // 3. Security Manager - منفصلة
  {
    title: 'Security Manager',
    icon: Shield,
    expandedByDefault: false,
    requiredPermissions: ['USER_READ', 'SUPER_ADMIN'],
    items: [
      { to: '/users', label: 'Users', icon: Users, perms: ['USER_READ'] },
      { to: '/roles', label: 'Roles', icon: ShieldCheck, perms: ['USER_READ'] },
      { to: '/groups', label: 'Groups', icon: Network, perms: ['USER_READ'] },
      { to: '/permissions', label: 'Permissions', icon: ShieldCheck, perms: ['USER_READ'] },
      { to: '/audit-logs', label: 'Audit Logs', icon: FileClock, perms: ['USER_READ'] },
      { to: '/security-policies', label: 'Security Policies', icon: LockKeyhole, perms: ['USER_READ'] },
    ],
  },
  // 4. Infrastructure Management - بدون HR
  {
    title: 'Infrastructure Management',
    icon: Building2,
    expandedByDefault: false,
    requiredPermissions: ['BRANCH_READ','MODULE_READ','ADMIN_DEPT_READ','SUPER_ADMIN'],
    items: [
      { to: '/branches', label: 'Branches', icon: Building2, perms: ['BRANCH_READ', 'SUPER_ADMIN'] },
      { to: '/modules', label: 'Modules', icon: Boxes, perms: ['MODULE_READ', 'SUPER_ADMIN'] },
      { to: '/administrative-departments', label: 'Administrative Departments', icon: Building2, perms: ['ADMIN_DEPT_READ', 'SUPER_ADMIN'] },
    ],
  },
  // 5. Core Data Management
  {
    title: 'Core Data Management',
    icon: GraduationCap,
    expandedByDefault: false,
    requiredPermissions: ['FACULTY_READ','BUILDING_READ','SEMESTER_READ','SUPER_ADMIN'],
    items: [
      { to: '/faculties', label: 'Faculties & Departments', icon: Flag, perms: ['FACULTY_READ', 'SUPER_ADMIN'] },
      { to: '/buildings', label: 'Classrooms & Buildings', icon: Warehouse, perms: ['BUILDING_READ', 'SUPER_ADMIN'] },
      { to: '/semesters', label: 'Semesters', icon: CalendarRange, perms: ['SEMESTER_READ', 'SUPER_ADMIN'] },
    ],
  },
]
