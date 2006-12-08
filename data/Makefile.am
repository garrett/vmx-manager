desktopdir = $(datadir)/applications
desktop_in_files=vmx-manager.desktop.in.in
desktop_DATA=$(desktop_in_files:.desktop.in.in=.desktop)

%.desktop.in: %.desktop.in.in
	sed -e "s/\@VERSION\@/$(VERSION)/" < $< > $@

@INTLTOOL_DESKTOP_RULE@

hicolordir = $(datadir)/icons/hicolor

install-data-local:
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/scalable/apps
	$(INSTALL_DATA) $(srcdir)/vmx-manager.svg $(DESTDIR)$(hicolordir)/scalable/apps/vmx-manager.svg
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/48x48/apps
	$(INSTALL_DATA) $(srcdir)/vmx-manager-48.png $(DESTDIR)$(hicolordir)/48x48/apps/vmx-manager.png
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/24x24/apps
	$(INSTALL_DATA) $(srcdir)/vmx-manager-24.png $(DESTDIR)$(hicolordir)/24x24/apps/vmx-manager.png


EXTRA_DIST =				\
	vmx-manager.glade		\
	vmx-manager.xml			\
	vmx-manager-config.xml		\
	operating-systems.csv		\
	defaults.vmx			\
	vmx-manager.desktop.in.in	\
	gnome-dev-cdrom.png		\
	gnome-dev-harddisk.png		\
	gnome-dev-ethernet.png		\
	gnome-dev-floppy.png		\
	vmx-manager.svg			\
	vmx-manager-48.png		\
	vmx-manager-24.png

DISTCLEANFILES = vmx-manager.desktop
