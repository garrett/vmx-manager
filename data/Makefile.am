desktopdir = $(datadir)/applications
desktop_in_files=vmx-manager.desktop.in.in
desktop_DATA=$(desktop_in_files:.desktop.in.in=.desktop)

%.desktop.in: %.desktop.in.in
	sed -e "s/\@VERSION\@/$(VERSION)/" < $< > $@

@INTLTOOL_DESKTOP_RULE@

hicolordir = $(datadir)/icons/hicolor
gtk_update_icon_cache_bin = "$$((which gtk-update-icon-cache || echo /opt/gnome/bin/gtk-update-icon-cache)2>/dev/null)"
gtk_update_icon_cache = $(gtk_update_icon_cache_bin) -f -t $(hicolordir)

install-data-local:
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/scalable/apps
	$(INSTALL_DATA) $(srcdir)/vmx-manager.svg $(DESTDIR)$(hicolordir)/scalable/apps/vmx-manager.svg
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/48x48/apps
	$(INSTALL_DATA) $(srcdir)/vmx-manager-48.png $(DESTDIR)$(hicolordir)/48x48/apps/vmx-manager.png
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/24x24/apps
	$(INSTALL_DATA) $(srcdir)/vmx-manager-24.png $(DESTDIR)$(hicolordir)/24x24/apps/vmx-manager.png
	@-if test -z "$(DESTDIR)"; then \
		if test -x $(gtk_update_icon_cache_bin); then \
			echo "Updating GTK icon cache"; \
			$(gtk_update_icon_cache); \
		else \
			echo "*** Icon cache not updated. Could not execute $(gtk_update_icon_cache_bin)"; \
		fi \
	else \
		echo "*** Icon cache not updated. After install, run this:"; \
		echo "***   $(gtk_update_icon_cache)"; \
	fi

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
