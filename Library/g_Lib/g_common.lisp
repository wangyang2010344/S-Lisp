{
	(let 
		`
			k
			build-cls
			build-m
			write-result
		`
		(p) args
		trans_name (load './trans_name.lisp)
		libs  
			(list
				(load './system.lisp)
				(load './better.lisp)
			)
		(cls fun)
			(reduce-right
				libs
				{
					(let (init lib) args)
					(kvs-reduce-right
						lib
						{
							(let 
								(init v k) args
								node (kvs-find1st v p.k)
							)
							(if-run (exist? node.run)
								{
									(let 
										key 
											(if-run (exist? v.alias)
												{v.alias}
												{(trans_name k)}
											)
										(ftype toStr)
											(if-run (exist? v.lisp)
												{['p.better-type {(stringify v.lisp)}]}
												{['p.in-type {k}]}
											)
									)
									(let (cls fun) init)
			                        (list
			                            (extend
			                                (str-join
			                                    (p.build-cls
			                                        [
			                                            key 'key
			                                            run 'node.run
			                                            other 'node.other
			                                            type 'ftype
			                                            toString 'toStr
			                                        ]
			                                    )
			                                )
			                                cls
			                            )
			                            (extend
			                                (str-join
			                                    (p.build-m k key)
			                                )
			                                fun
			                            )
			                        )
								}
								{init}
							)
						}
						init
					)
				}
				[[] []]
			)
	)
	(p.write-result cls fun)
}