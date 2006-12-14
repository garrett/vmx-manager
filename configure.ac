AC_INIT(src/EntryPoint.cs)

AM_INIT_AUTOMAKE(vmx-manager, 0.2.0)
AM_MAINTAINER_MODE

AC_PROG_INTLTOOL([0.21])

PKG_CHECK_MODULES(VMXMAN, gtk-sharp-2.0 glade-sharp-2.0 gnome-sharp-2.0)
VMXMAN_LIBS="$VMXMAN_LIBS -r:Mono.Posix"
AC_SUBST(VMXMAN_LIBS)

dnl Check for Mono
AC_PATH_PROG(MCS, gmcs)
AC_PATH_PROG(MONO, mono)

MCS_FLAGS="-codepage:utf8 -debug"
AC_SUBST(MCS_FLAGS)

dnl Intl
ALL_LINGUAS=""
GETTEXT_PACKAGE=vmx-manager
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE,"$GETTEXT_PACKAGE", [Gettext package])
AM_GLIB_GNU_GETTEXT

AC_OUTPUT([
Makefile
dbus-sharp/Makefile
dbus-sharp/NDesk.DBus.GLib.dll.config
hal-sharp/Makefile
src/Makefile
src/vmx-manager
data/Makefile
po/Makefile.in
])
